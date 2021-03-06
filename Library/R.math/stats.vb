﻿#Region "Microsoft.VisualBasic::59dce2bf9eac4dec3cb2c1e640a7f913, Library\R.math\stats.vb"

    ' Author:
    ' 
    '       asuka (amethyst.asuka@gcmodeller.org)
    '       xie (genetics@smrucc.org)
    '       xieguigang (xie.guigang@live.com)
    ' 
    ' Copyright (c) 2018 GPL3 Licensed
    ' 
    ' 
    ' GNU GENERAL PUBLIC LICENSE (GPL3)
    ' 
    ' 
    ' This program is free software: you can redistribute it and/or modify
    ' it under the terms of the GNU General Public License as published by
    ' the Free Software Foundation, either version 3 of the License, or
    ' (at your option) any later version.
    ' 
    ' This program is distributed in the hope that it will be useful,
    ' but WITHOUT ANY WARRANTY; without even the implied warranty of
    ' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    ' GNU General Public License for more details.
    ' 
    ' You should have received a copy of the GNU General Public License
    ' along with this program. If not, see <http://www.gnu.org/licenses/>.



    ' /********************************************************************************/

    ' Summaries:

    ' Module stats
    ' 
    '     Constructor: (+1 Overloads) Sub New
    '     Function: asDist, corr, dataframeRow, dist, fisher_test
    '               getQuantileLevels, matrixDataFrame, prcomp, printMatrix, printTtest
    '               printTwoSampleTTest, spline, tabulateMode, ttest
    ' 
    ' Enum SplineAlgorithms
    ' 
    '     Bezier, BSpline, CatmullRom, CubiSpline
    ' 
    '  
    ' 
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.ComponentModel.Collection.Generic
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Data.csv.IO
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Math.DataFrame
Imports Microsoft.VisualBasic.Math.Distributions
Imports Microsoft.VisualBasic.Math.LinearAlgebra.Prcomp
Imports Microsoft.VisualBasic.Math.Quantile
Imports Microsoft.VisualBasic.Math.Statistics.Hypothesis
Imports Microsoft.VisualBasic.Math.Statistics.Hypothesis.FishersExact
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Runtime.Interop
Imports Rdataframe = SMRUCC.Rsharp.Runtime.Internal.Object.dataframe
Imports REnv = SMRUCC.Rsharp.Runtime

''' <summary>
''' 
''' </summary>
<Package("stats")>
Module stats

    Sub New()
        Internal.ConsolePrinter.AttachConsoleFormatter(Of DistanceMatrix)(AddressOf printMatrix)
        Internal.ConsolePrinter.AttachConsoleFormatter(Of TtestResult)(AddressOf printTtest)
        Internal.ConsolePrinter.AttachConsoleFormatter(Of TwoSampleResult)(AddressOf printTwoSampleTTest)
        Internal.ConsolePrinter.AttachConsoleFormatter(Of FishersExactPvalues)(Function(o) o.ToString)

        Internal.Object.Converts.makeDataframe.addHandler(GetType(DataMatrix), AddressOf matrixDataFrame)
        Internal.Object.Converts.makeDataframe.addHandler(GetType(DistanceMatrix), AddressOf matrixDataFrame)
        Internal.Object.Converts.makeDataframe.addHandler(GetType(CorrelationMatrix), AddressOf matrixDataFrame)
    End Sub

    Private Function matrixDataFrame(x As DataMatrix, args As list, env As Environment) As Rdataframe
        Dim table As New Rdataframe With {
            .columns = New Dictionary(Of String, Array),
            .rownames = x.keys
        }

        For Each row In x.PopulateRowObjects(Of DataSet)
            Call table.columns.Add(row.ID, row(table.rownames))
        Next

        Return table
    End Function

    Private Function printTtest(t As TtestResult) As String
        Return t.ToString
    End Function

    Private Function printTwoSampleTTest(t As TwoSampleResult) As String
        Return t.ToString
    End Function

    Private Function printMatrix(d As DistanceMatrix) As String
        Dim sb As New StringBuilder

        Call sb.AppendLine($"Distance matrix of {d.keys.Length} objects:")
        Call sb.AppendLine(d.ToString)
        Call sb.AppendLine()

        For Each row In d.PopulateRowObjects(Of DataSet).Take(6)
            Call sb.AppendLine($"{row.ID}: {row.Properties.Take(8).Select(Function(t) $"{t.Key}:{t.Value.ToString("F2")}").JoinBy(", ")} ...")
        Next

        Call sb.AppendLine("...")

        Return sb.ToString
    End Function

    ''' <summary>
    ''' Interpolating Splines
    ''' </summary>
    ''' <returns></returns>
    <ExportAPI("spline")>
    Public Function spline(<RRawVectorArgument> data As Object,
                           Optional algorithm As SplineAlgorithms = SplineAlgorithms.BSpline,
                           Optional env As Environment = Nothing) As Object

        If data Is Nothing Then
            Return Nothing
        End If

        Select Case algorithm
            Case SplineAlgorithms.Bezier
            Case SplineAlgorithms.BSpline
            Case SplineAlgorithms.CatmullRom
            Case SplineAlgorithms.CubiSpline

        End Select

        Return Internal.debug.stop($"unsupported spline algorithm: {algorithm.ToString}", env)
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="x"></param>
    ''' <returns></returns>
    <ExportAPI("tabulate.mode")>
    Public Function tabulateMode(<RRawVectorArgument> x As Object) As Double
        Return REnv _
            .asVector(Of Double)(x) _
            .DoCall(Function(vec)
                        Return Bootstraping.TabulateMode(DirectCast(vec, Double()))
                    End Function)
    End Function

    ''' <summary>
    ''' ### Principal Components Analysis
    ''' 
    ''' Performs a principal components analysis on the given data matrix 
    ''' and returns the results as an object of class ``prcomp``.
    ''' 
    ''' The calculation is done by a singular value decomposition of the 
    ''' (centered and possibly scaled) data matrix, not by using eigen on 
    ''' the covariance matrix. This is generally the preferred method for 
    ''' numerical accuracy. The print method for these objects prints the 
    ''' results in a nice format and the plot method produces a scree 
    ''' plot.
    '''
    ''' Unlike princomp, variances are computed With the usual divisor N - 1.
    ''' Note that scale = True cannot be used If there are zero Or constant 
    ''' (For center = True) variables.
    ''' </summary>
    ''' <param name="x">
    ''' a numeric or complex matrix (or data frame) which provides the 
    ''' data for the principal components analysis.
    ''' </param>
    ''' <param name="center">
    ''' a logical value indicating whether the variables should be shifted 
    ''' to be zero centered. Alternately, a vector of length equal the 
    ''' number of columns of x can be supplied. The value is passed to scale.
    ''' </param>
    ''' <param name="scale">
    ''' a logical value indicating whether the variables should be scaled to 
    ''' have unit variance before the analysis takes place. The default is 
    ''' FALSE for consistency with S, but in general scaling is advisable. 
    ''' Alternatively, a vector of length equal the number of columns of x 
    ''' can be supplied. The value is passed to scale.
    ''' </param>
    ''' <returns></returns>
    ''' <remarks>
    ''' The signs of the columns of the rotation matrix are arbitrary, and 
    ''' so may differ between different programs for PCA, and even between 
    ''' different builds of R.
    ''' </remarks>
    <ExportAPI("prcomp")>
    <RApiReturn(GetType(PCA))>
    Public Function prcomp(<RRawVectorArgument>
                           x As Object,
                           Optional scale As Boolean = False,
                           Optional center As Boolean = False,
                           Optional env As Environment = Nothing) As Object
        If x Is Nothing Then
            Return Internal.debug.stop("'data' must be of a vector type, was 'NULL'", env)
        End If

        Dim matrix As Double()()

        If TypeOf x Is Rdataframe Then
            With DirectCast(x, Rdataframe)
                matrix = .nrows _
                    .Sequence _
                    .Select(Function(i)
                                Return REnv.asVector(Of Double)(.getRowList(i, drop:=False))
                            End Function) _
                    .Select(Function(v) DirectCast(v, Double())) _
                    .ToArray
            End With
        Else
            Throw New NotImplementedException
        End If

        Dim PCA As New PCA(matrix, center, scale)

        Return PCA
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="x"></param>
    ''' <param name="is_matrix"></param>
    ''' <param name="projection">
    ''' argument for extract data from the given data object, this parameter is depends 
    ''' on the <paramref name="is_matrix"/> argument, for:
    ''' 
    ''' + is_matrix: means the given data is a matrix liked data, then ``type`` should be exists.
    ''' + not is_matrix: means the given data is a tabular data for represents the data matrix, 
    '''                  then slots ``f1``, ``f2``, and ``val`` should be exists.
    ''' </param>
    ''' <param name="env"></param>
    ''' <returns></returns>
    <ExportAPI("as.dist")>
    Public Function asDist(x As Rdataframe,
                           Optional is_matrix As Boolean = True,
                           <RListObjectArgument>
                           Optional projection As list = Nothing,
                           Optional env As Environment = Nothing) As DistanceMatrix

        If is_matrix Then
            Dim matrix As Double()() = x.columns _
                .Select(Function(a)
                            Return a.Value _
                                .AsObjectEnumerator(Of String) _
                                .Select(AddressOf ParseDouble) _
                                .ToArray
                        End Function) _
                .ToArray
            Dim dist As Double()() = New Double(x.nrows - 1)() {}
            Dim is_dist As Boolean = projection!type <> "cor"
            Dim j As Integer

            For i As Integer = 0 To x.nrows - 1
                j = i
                dist(i) = matrix.Select(Function(col) col(j)).ToArray
            Next

            Return New DistanceMatrix(x.rownames, dist, is_dist)
        Else
            Dim raw As EntityObject() = x.getRowNames _
                .Select(Function(id, index)
                            Return x.dataframeRow(Of String, EntityObject)(id, index)
                        End Function) _
                .ToArray
            Dim item1$ = projection!f1
            Dim item2$ = projection!f2
            Dim val$ = projection!val

            Return LoadDataMatrix.FromTabular(raw, item1, item2, val)
        End If
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <typeparam name="T"></typeparam>
    ''' <typeparam name="DataSet"></typeparam>
    ''' <param name="x"></param>
    ''' <param name="id"></param>
    ''' <param name="index">the row index</param>
    ''' <returns></returns>
    <Extension>
    Private Function dataframeRow(Of T, DataSet As {New, INamedValue, DynamicPropertyBase(Of T)})(x As Rdataframe, id As String, index%) As DataSet
        Dim row As Dictionary(Of String, Object) = x.getRowList(index, drop:=True)
        Dim props As Dictionary(Of String, T) = row _
            .ToDictionary(Function(a) a.Key,
                            Function(a)
                                Return CType(REnv.single(a.Value), T)
                            End Function)

        Return New DataSet With {
            .Key = id,
            .Properties = props
        }
    End Function

    ''' <summary>
    ''' matrix correlation
    ''' </summary>
    ''' <param name="x">evaluate correlation for each row elements</param>
    ''' <returns></returns>
    <ExportAPI("corr")>
    Public Function corr(x As Rdataframe, Optional spearman As Boolean = False) As CorrelationMatrix
        Dim rows As DataSet() = x.getRowNames _
            .Select(Function(id, index)
                        Return x.dataframeRow(Of Double, DataSet)(id, index)
                    End Function) _
            .ToArray
        Dim cor As CorrelationMatrix = rows.Correlation(spearman)

        Return cor
    End Function

    ''' <summary>
    ''' get quantile levels
    ''' </summary>
    ''' <param name="q"></param>
    ''' <param name="level"></param>
    ''' <param name="env"></param>
    ''' <returns></returns>
    <ExportAPI("level")>
    Public Function getQuantileLevels(q As QuantileQuery, <RRawVectorArgument> level As Object, Optional env As Environment = Nothing) As Object
        Dim levels As Double() = REnv.asVector(Of Double)(level)
        Dim result As Double() = levels.Select(AddressOf q.Query).ToArray

        Return result
    End Function

    ''' <summary>
    ''' ### Distance Matrix Computation
    ''' 
    ''' This function computes and returns the distance matrix computed by using 
    ''' the specified distance measure to compute the distances between the rows 
    ''' of a data matrix.
    ''' </summary>
    ''' <param name="x">a numeric matrix, data frame or "dist" object.</param>
    ''' <param name="method">
    ''' the distance measure to be used. This must be one of "euclidean", "maximum", 
    ''' "manhattan", "canberra", "binary" or "minkowski". Any unambiguous substring 
    ''' can be given.
    ''' </param>
    ''' <param name="diag"></param>
    ''' <param name="upper"></param>
    ''' <param name="p%"></param>
    ''' <param name="env"></param>
    ''' <returns></returns>
    ''' <remarks>
    ''' Available distance measures are (written for two vectors x and y):
    '''
    ''' + euclidean:
    ''' Usual distance between the two vectors (2 norm aka L_2), sqrt(sum((x_i - y_i)^2)).
    '''
    ''' + maximum:
    ''' Maximum distance between two components Of x And y (supremum norm)
    '''
    ''' + manhattan:
    ''' Absolute distance between the two vectors (1 norm aka L_1).
    '''
    ''' + canberra:
    ''' ``sum(|x_i - y_i| / (|x_i| + |y_i|))``. Terms with zero numerator And 
    ''' denominator are omitted from the sum And treated as if the values were 
    ''' missing.
    '''
    ''' This Is intended for non-negative values (e.g., counts), in which case the 
    ''' denominator can be written in various equivalent ways; Originally, R used 
    ''' ``x_i + y_i``, then from 1998 to 2017, |x_i + y_i|, And then the correct 
    ''' ``|x_i| + |y_i|``.
    '''
    ''' + binary:
    ''' (aka asymmetric binary): The vectors are regarded As binary bits, so non-zero 
    ''' elements are 'on’ and zero elements are ‘off’. The distance is the proportion 
    ''' of bits in which only one is on amongst those in which at least one is on.
    '''
    ''' + minkowski:
    ''' The p norm, the pth root Of the sum Of the pth powers Of the differences 
    ''' Of the components.
    '''
    ''' Missing values are allowed, And are excluded from all computations involving 
    ''' the rows within which they occur. Further, When Inf values are involved, 
    ''' all pairs Of values are excluded When their contribution To the distance gave 
    ''' NaN Or NA. If some columns are excluded In calculating a Euclidean, Manhattan, 
    ''' Canberra Or Minkowski distance, the sum Is scaled up proportionally To the 
    ''' number Of columns used. If all pairs are excluded When calculating a 
    ''' particular distance, the value Is NA.
    '''
    ''' The "dist" method of as.matrix() And as.dist() can be used for conversion 
    ''' between objects of class "dist" And conventional distance matrices.
    '''
    ''' ``as.dist()`` Is a generic function. Its default method handles objects inheriting 
    ''' from class "dist", Or coercible to matrices using as.matrix(). Support for 
    ''' classes representing distances (also known as dissimilarities) can be added 
    ''' by providing an as.matrix() Or, more directly, an as.dist method for such 
    ''' a class.
    ''' </remarks>
    <ExportAPI("dist")>
    <RApiReturn(GetType(DistanceMatrix))>
    Public Function dist(<RRawVectorArgument> x As Object,
                         Optional method$ = "euclidean",
                         Optional diag As Boolean = False,
                         Optional upper As Boolean = False,
                         Optional p% = 2,
                         Optional env As Environment = Nothing) As Object

        Dim raw As DataSet()

        If x Is Nothing Then
            Return Nothing
        ElseIf TypeOf x Is Rdataframe Then
            With DirectCast(x, Rdataframe)
                raw = .rownames _
                    .Select(Function(name, i)
                                Return .dataframeRow(Of Double, DataSet)(name, i)
                            End Function) _
                    .ToArray
            End With
        ElseIf TypeOf x Is DataSet() Then
            raw = x
        Else
            Return Internal.debug.stop(New InvalidCastException(x.GetType.FullName), env)
        End If

        Select Case Strings.LCase(method)
            Case "euclidean"
                Return raw.Euclidean
            Case Else
                Return Internal.debug.stop(New NotImplementedException(method), env)
        End Select
    End Function

    ''' <summary>
    ''' Student's t-Test
    ''' 
    ''' Performs one and two sample t-tests on vectors of data.
    ''' </summary>
    ''' <param name="x">a (non-empty) numeric vector of data values.</param>
    ''' <param name="y">an optional (non-empty) numeric vector of data values.
    ''' </param>
    ''' <param name="alternative">
    ''' a character string specifying the alternative hypothesis, must be one of 
    ''' "two.sided" (default), "greater" or "less". You can specify just the initial 
    ''' letter.
    ''' </param>
    ''' <param name="mu">
    ''' a number indicating the true value of the mean (or difference in means if you 
    ''' are performing a two sample test).
    ''' </param>
    ''' <param name="paired">	
    ''' a logical indicating whether you want a paired t-test.</param>
    ''' <param name="var_equal">
    ''' a logical variable indicating whether to treat the two variances as being equal. 
    ''' If TRUE then the pooled variance is used to estimate the variance otherwise the 
    ''' Welch (or Satterthwaite) approximation to the degrees of freedom is used.
    ''' </param>
    ''' <param name="conf_level">
    ''' confidence level of the interval.
    ''' </param>
    ''' <returns></returns>
    <ExportAPI("t.test")>
    <RApiReturn(GetType(TwoSampleResult), GetType(TtestResult))>
    Public Function ttest(<RRawVectorArgument> x As Object, <RRawVectorArgument> Optional y As Object = Nothing,
                          Optional alternative As Hypothesis = Hypothesis.TwoSided,
                          Optional mu# = 0,
                          Optional paired As Boolean = False,
                          Optional var_equal As Boolean = False,
                          Optional conf_level# = 0.95) As Object

        If y Is Nothing Then
            Return t.Test(REnv.asVector(Of Double)(x), alternative, mu, alpha:=1 - conf_level)
        Else
            Return t.Test(REnv.asVector(Of Double)(x), REnv.asVector(Of Double)(y), alternative, mu, alpha:=1 - conf_level, varEqual:=var_equal)
        End If
    End Function

    ''' <summary>
    ''' Fisher's Exact Test for Count Data
    ''' 
    ''' Performs Fisher's exact test for testing the null of independence 
    ''' of rows and columns in a contingency table with fixed marginals.
    ''' </summary>
    ''' <param name="a%"></param>
    ''' <param name="b%"></param>
    ''' <param name="c%"></param>
    ''' <param name="d%"></param>
    ''' <returns></returns>
    <ExportAPI("fisher.test")>
    Public Function fisher_test(a%, b%, c%, d%) As FishersExactPvalues
        Return FishersExactTest.FishersExact(a, b, c, d)
    End Function
End Module

Public Enum SplineAlgorithms
    BSpline
    CubiSpline
    CatmullRom
    Bezier
End Enum
