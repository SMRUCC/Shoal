﻿#Region "Microsoft.VisualBasic::b1b18eab84eda50dec4385ec39b46862, R.base\utils\dataframe.vb"

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

    ' Module dataframe
    ' 
    '     Function: colnames, project, readDataSet
    ' 
    ' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Data.csv.IO
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.Rsharp.Runtime

''' <summary>
''' The sciBASIC.NET dataframe api
''' </summary>
<Package("dataframe", Category:=APICategories.UtilityTools)>
Module dataframe

    <ExportAPI("dataset.colnames")>
    Public Function colnames(dataset As Array, envir As Environment) As Object
        Dim baseElement As Type = dataset.GetValue(Scan0).GetType

        If baseElement Is GetType(EntityObject) Then
            Return dataset.AsObjectEnumerator _
                .Select(Function(d)
                            Return DirectCast(d, EntityObject)
                        End Function) _
                .PropertyNames
        ElseIf baseElement Is GetType(DataSet) Then
            Return dataset.AsObjectEnumerator _
                .Select(Function(d)
                            Return DirectCast(d, DataSet)
                        End Function) _
                .PropertyNames
        Else
            Return Internal.debug.stop(New InvalidProgramException, envir)
        End If
    End Function

    ''' <summary>
    ''' Get/set value of a given data column
    ''' </summary>
    ''' <param name="dataset"></param>
    ''' <param name="col$"></param>
    ''' <param name="values"></param>
    ''' <param name="envir"></param>
    ''' <returns></returns>
    <ExportAPI("dataset.vector")>
    Public Function vector(dataset As Array, col$, Optional values As Array = Nothing, Optional envir As Environment = Nothing) As Object
        Dim baseElement As Type = dataset.GetValue(Scan0).GetType
        Dim vectors As New List(Of Object)()
        Dim isGetter As Boolean = False
        Dim getValue As Func(Of Object) = Nothing

        If values Is Nothing Then
            isGetter = True
        ElseIf values.Length = 1 Then
            Dim firstValue As Object = Runtime.getFirst(values)
            getValue = Function() firstValue
        Else
            Dim populator As IEnumerator = values.GetEnumerator
            getValue = Function() As Object
                           populator.MoveNext()
                           Return populator.Current
                       End Function
        End If

        If Not isGetter Then
            vectors = New List(Of Object)(values.AsObjectEnumerator)
        End If

        If baseElement Is GetType(EntityObject) Then
            For Each item As EntityObject In Runtime.asVector(Of EntityObject)(dataset)
                If isGetter Then
                    vectors.Add(item(col))
                Else
                    item(col) = Scripting.ToString(getValue())
                End If
            Next
        ElseIf baseElement Is GetType(DataSet) Then
            For Each item As DataSet In Runtime.asVector(Of DataSet)(dataset)
                If isGetter Then
                    vectors.Add(item(col))
                Else
                    item(col) = Conversion.CTypeDynamic(Of Double)(getValue())
                End If
            Next
        Else
            Return Internal.debug.stop(New InvalidProgramException, envir)
        End If

        Return vectors.ToArray
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="dataset"></param>
    ''' <param name="cols"></param>
    ''' <returns></returns>
    <ExportAPI("dataset.project")>
    Public Function project(dataset As Array, cols As String(), envir As Environment) As Object
        Dim baseElement As Type = dataset.GetValue(Scan0).GetType

        If baseElement Is GetType(EntityObject) Then
            Return dataset.AsObjectEnumerator _
                .Select(Function(d)
                            Dim row As EntityObject = DirectCast(d, EntityObject)
                            Dim subset As New EntityObject With {
                                .ID = row.ID,
                                .Properties = row.Properties.Subset(cols)
                            }

                            Return subset
                        End Function) _
                .ToArray
        ElseIf baseElement Is GetType(DataSet) Then
            Return dataset.AsObjectEnumerator _
                .Select(Function(d)
                            Dim row As DataSet = DirectCast(d, DataSet)
                            Dim subset As New DataSet With {
                                .ID = row.ID,
                                .Properties = row.Properties.Subset(cols)
                            }

                            Return subset
                        End Function) _
                .ToArray
        Else
            Return Internal.debug.stop(New InvalidProgramException, envir)
        End If
    End Function

    <ExportAPI("read.dataframe")>
    Public Function readDataSet(file$, Optional mode$ = "numeric|character") As Object
        Dim readMode = mode.Split("|"c).First

        Select Case readMode.ToLower
            Case "numeric"
                Return DataSet.LoadDataSet(file).ToArray
            Case "character"
                Return EntityObject.LoadDataSet(file).ToArray
            Case Else
                Return utils.read_csv(file)
        End Select
    End Function
End Module
