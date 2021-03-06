﻿#Region "Microsoft.VisualBasic::ce7470fda6511b384f56fe6737fbec01, Library\R.base\utils\dataframe.vb"

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
'     Constructor: (+1 Overloads) Sub New
'     Function: appendCells, appendRow, asIndexList, cells, colnames
'               column, CreateRowObject, dataframeTable, deserialize, measureColumnVector
'               openCsv, printTable, project, rawToDataFrame, readCsvRaw
'               readDataSet, rows, RowToString, transpose, vector
' 
' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Logging
Imports Microsoft.VisualBasic.ApplicationServices.Terminal
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.ComponentModel.Collection.Generic
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Data.csv
Imports Microsoft.VisualBasic.Data.csv.IO
Imports Microsoft.VisualBasic.Data.csv.StorageProvider.Reflection
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.Rsharp.Interpreter
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Runtime.Internal.Object.Converts
Imports SMRUCC.Rsharp.Runtime.Interop
Imports csv = Microsoft.VisualBasic.Data.csv.IO.File
Imports Idataframe = Microsoft.VisualBasic.Data.csv.IO.DataFrame
Imports Rdataframe = SMRUCC.Rsharp.Runtime.Internal.Object.dataframe
Imports REnv = SMRUCC.Rsharp.Runtime
Imports RPrinter = SMRUCC.Rsharp.Runtime.Internal.ConsolePrinter
Imports Rsharp = SMRUCC.Rsharp

''' <summary>
''' The sciBASIC.NET dataframe api
''' </summary>
<Package("dataframe", Category:=APICategories.UtilityTools, Publisher:="xie.guigang@gcmodeller.org")>
Module dataframe

    Sub New()
        Call RPrinter.AttachConsoleFormatter(Of DataSet)(AddressOf RowToString)
        Call RPrinter.AttachConsoleFormatter(Of DataSet())(AddressOf printTable)
        Call RPrinter.AttachConsoleFormatter(Of EntityObject())(AddressOf printTable)
        ' Call RPrinter.AttachConsoleFormatter(Of csv)(AddressOf printTable)
        Call RPrinter.AttachConsoleFormatter(Of RowObject)(AddressOf printRowVector)

        Call makeDataframe.addHandler(GetType(DataSet()), AddressOf dataframeTable(Of Double, DataSet))
        Call makeDataframe.addHandler(GetType(EntityObject()), AddressOf dataframeTable(Of String, EntityObject))
    End Sub

    Private Function printRowVector(r As RowObject) As String
        Dim cellsDataPreviews As String

        If r.NumbersOfColumn > 6 Then
            cellsDataPreviews = r _
                .Take(6) _
                .Select(Function(str) $"""{str}""") _
                .JoinBy(vbTab) & vbTab & "..."
        Else
            cellsDataPreviews = r.Select(Function(str) $"""{str}""").JoinBy(vbTab)
        End If

        Return $"row, char [1:{r.NumbersOfColumn}] {cellsDataPreviews}"
    End Function

    Private Function dataframeTable(Of T, DataSet As {INamedValue, DynamicPropertyBase(Of T)})(data As DataSet(), args As list, env As Environment) As Rdataframe
        Dim names As String() = data.Keys.ToArray
        Dim table As New Rdataframe With {
            .rownames = names,
            .columns = New Dictionary(Of String, Array)
        }

        For Each colName As String In data.PropertyNames
            table.columns(colName) = data _
                .Select(Function(d) d(colName)) _
                .ToArray
        Next

        Return table
    End Function

    Private Function printTable(obj As Object) As String
        Dim matrix As csv

        Select Case obj.GetType
            Case GetType(DataSet())
                matrix = DirectCast(obj, DataSet()).ToCsvDoc
            Case GetType(EntityObject())
                matrix = DirectCast(obj, EntityObject()).ToCsvDoc
                ' Case GetType(csv)
                ' matrix = DirectCast(obj, csv)
            Case Else
                Throw New NotImplementedException
        End Select

        Return matrix _
            .AsMatrix _
            .Select(Function(r) r.ToArray) _
            .Print(False)
    End Function

    Private Function RowToString(x As Object) As String
        Dim id$, length%
        Dim keys$()

        If x.GetType Is GetType(DataSet) Then
            With DirectCast(x, DataSet)
                id = .ID
                length = .Properties.Count
                keys = .Properties _
                       .Keys _
                       .ToArray
            End With
        Else
            With DirectCast(x, EntityObject)
                id = .ID
                length = .Properties.Count
                keys = .Properties _
                       .Keys _
                       .ToArray
            End With
        End If

        Return $"${id} {length} slots {{{keys.Take(3).JoinBy(", ")}..."
    End Function

    ''' <summary>
    ''' Load .NET objects from a given dataframe data object.
    ''' </summary>
    ''' <param name="data"></param>
    ''' <param name="type"></param>
    ''' <param name="env"></param>
    ''' <returns></returns>
    <ExportAPI("as.objects")>
    <RApiReturn(GetType(vector))>
    Public Function deserialize(data As csv, type As Object,
                                Optional strict As Boolean = False,
                                Optional metaBlank$ = "",
                                Optional silent As Boolean = True,
                                Optional env As Environment = Nothing) As Object

        Dim dataframe As Idataframe = Idataframe.CreateObject(data)
        Dim schema As RType = env.globalEnvironment.GetType(type)
        Dim result As IEnumerable(Of Object) = dataframe.LoadDataToObject(
            type:=schema.raw,
            strict:=strict,
            metaBlank:=metaBlank,
            silent:=silent
        )
        Dim vector As New vector(schema, result, env)

        Return vector
    End Function

    <Extension>
    Private Function measureColumnVector(col As String(), check_modes As Boolean) As Array
        If check_modes Then
            Return col _
                .Skip(1) _
                .ToArray _
                .DoCall(AddressOf IO.DataImports.ParseVector)
        Else
            Return DirectCast(col.Skip(1).ToArray, Array)
        End If
    End Function

    ''' <summary>
    ''' convert the raw csv table object to R dataframe object.
    ''' </summary>
    ''' <param name="raw"></param>
    ''' <returns></returns>
    <ExportAPI("rawToDataFrame")>
    <Extension>
    <RApiReturn(GetType(Rdataframe))>
    Public Function rawToDataFrame(raw As csv,
                                   <RRawVectorArgument>
                                   Optional row_names As Object = Nothing,
                                   Optional check_names As Boolean = True,
                                   Optional check_modes As Boolean = True,
                                   Optional env As Environment = Nothing) As Object

        Dim cols() = raw.Columns.ToArray
        Dim colNames As String() = cols.Select(Function(col) col(Scan0)).ToArray

        If check_names Then
            colNames = Internal.Invokes.base.makeNames(colNames, unique:=True)
        Else
            colNames = colNames.uniqueNames
        End If

        Dim dataframe As New Rdataframe() With {
            .columns = cols _
                .SeqIterator _
                .AsParallel _
                .Select(Function(i) (i.i, colVec:=i.value.measureColumnVector(check_modes))) _
                .OrderBy(Function(i) i.i) _
                .ToDictionary(Function(col) colNames(col.i),
                              Function(col)
                                  Return col.colVec
                              End Function)
        }

        If Not row_names Is Nothing Then
            Dim err As New Value(Of Message)

            row_names = ensureRowNames(row_names, env)

            If Program.isException(row_names) Then
                Return row_names
            End If
            If Not err = dataframe.setRowNames(row_names, env) Is Nothing Then
                Return err.Value
            End If
        End If

        Return dataframe
    End Function

    ''' <summary>
    ''' raw dataframe to rows
    ''' </summary>
    ''' <param name="file"></param>
    ''' <returns></returns>
    <ExportAPI("rows")>
    Public Function rows(file As File) As RowObject()
        Return file.ToArray
    End Function

    ''' <summary>
    ''' raw dataframe get column data by index
    ''' </summary>
    ''' <param name="file"></param>
    ''' <param name="index"></param>
    ''' <returns></returns>
    <ExportAPI("column")>
    Public Function column(file As File, index As Integer) As String()
        Return file.Column(index).ToArray
    End Function

    <ExportAPI("cells")>
    Public Function cells(x As Object, Optional env As Environment = Nothing) As Object
        Dim type As Type

        If x Is Nothing Then
            Return New String() {}
        Else
            type = x.GetType
        End If

        Select Case type
            Case GetType(RowObject)
                Return DirectCast(x, RowObject).ToArray
            Case GetType(DataSet)
                Return DirectCast(x, DataSet).Properties.Values.ToArray
            Case GetType(EntityObject)
                Return DirectCast(x, EntityObject).Properties.Values.ToArray
            Case Else
                Return Internal.debug.stop(New InvalidCastException(type.FullName), env)
        End Select
    End Function

    <ExportAPI("read.csv.raw")>
    Public Function readCsvRaw(file$, Optional encoding As Object = "") As File
        Return csv.Load(file, Rsharp.GetEncoding(encoding))
    End Function

    <ExportAPI("open.csv")>
    Public Function openCsv(file$, Optional encoding As Object = "") As RDispose
        Dim table As New File
        Dim textEncode As Encoding = Rsharp.GetEncoding(encoding)

        Return New RDispose(table, Sub() table.Save(file, textEncode))
    End Function

    <ExportAPI("row")>
    Public Function CreateRowObject(Optional vals As Array = Nothing) As RowObject
        Return New RowObject(vals.AsObjectEnumerator.Select(AddressOf Scripting.ToString))
    End Function

    <ExportAPI("append.cells")>
    Public Function appendCells(row As RowObject, cells As Array) As RowObject
        row.AddRange(cells.AsObjectEnumerator.Select(AddressOf Scripting.ToString))
        Return row
    End Function

    <ExportAPI("append.row")>
    Public Function appendRow(table As File, row As RowObject) As File
        table.Add(row)
        Return table
    End Function

    ''' <summary>
    ''' Get/Set column names of the given <paramref name="dataset"/>
    ''' </summary>
    ''' <param name="dataset"></param>
    ''' <param name="values"></param>
    ''' <param name="envir"></param>
    ''' <returns></returns>
    <ExportAPI("dataset.colnames")>
    Public Function colnames(dataset As Array, <RByRefValueAssign> Optional values As Array = Nothing, Optional envir As Environment = Nothing) As Object
        Dim baseElement As Type = REnv.MeasureArrayElementType(dataset)

        If values Is Nothing OrElse values.Length = 0 Then
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
        Else
            Dim names As String() = DirectCast(REnv.asVector(Of String)(values), String())

            If baseElement Is GetType(EntityObject) Then
                Return dataset.AsObjectEnumerator(Of EntityObject) _
                    .ColRenames(names) _
                    .Select(Function(a)
                                Return DirectCast(a, EntityObject)
                            End Function) _
                    .ToArray
            Else
                Return dataset.AsObjectEnumerator(Of DataSet) _
                    .ColRenames(names) _
                    .Select(Function(a)
                                Return DirectCast(a, DataSet)
                            End Function) _
                    .ToArray
            End If
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
    Public Function vector(dataset As Array, col$, <RByRefValueAssign> Optional values As Array = Nothing, Optional envir As Environment = Nothing) As Object
        If dataset Is Nothing OrElse dataset.Length = 0 Then
            Return Nothing
        End If

        Dim baseElement As Type = REnv.MeasureArrayElementType(dataset)
        Dim vectors As New List(Of Object)()
        Dim isGetter As Boolean = False
        Dim getValue As Func(Of Object) = Nothing

        If values Is Nothing Then
            isGetter = True
        ElseIf values.Length = 1 Then
            Dim firstValue As Object = REnv.getFirst(values)
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
            For Each item As EntityObject In REnv.asVector(Of EntityObject)(dataset)
                If isGetter Then
                    vectors.Add(item(col))
                Else
                    item(col) = Scripting.ToString(getValue())
                End If
            Next
        ElseIf baseElement Is GetType(DataSet) Then
            For Each item As DataSet In REnv.asVector(Of DataSet)(dataset)
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
    ''' Subset of the given dataframe by columns
    ''' </summary>
    ''' <param name="dataset"></param>
    ''' <param name="cols">a character vector of the dataframe column names.</param>
    ''' <returns></returns>
    <ExportAPI("dataset.project")>
    Public Function project(dataset As Array, cols$(), Optional envir As Environment = Nothing) As Object
        Dim baseElement As Type = REnv.MeasureArrayElementType(dataset)

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

    <ExportAPI("dataset.transpose")>
    Public Function transpose(dataset As Array, Optional env As Environment = Nothing) As Object
        Dim baseElement As Type = REnv.MeasureArrayElementType(dataset)

        If baseElement Is GetType(EntityObject) Then
            Return dataset.AsObjectEnumerator(Of EntityObject).Transpose
        ElseIf baseElement Is GetType(DataSet) Then
            Return dataset.AsObjectEnumerator(Of DataSet).Transpose
        Else
            Return Internal.debug.stop(New InvalidProgramException, env)
        End If
    End Function

    <ExportAPI("as.index")>
    <RApiReturn(GetType(list))>
    Public Function asIndexList(<RRawVectorArgument> dataframe As Object, Optional env As Environment = Nothing) As Object
        Dim data As pipeline = pipeline.TryCreatePipeline(Of EntityObject)(dataframe, env, suppress:=True)

        If data.isError Then
            data = pipeline.TryCreatePipeline(Of DataSet)(dataframe, env)

            If data.isError Then
                Return data.getError
            End If

            Return data.populates(Of DataSet)(env) _
                .ToDictionary(Function(a) a.ID, Function(a) CObj(a)) _
                .DoCall(Function(index)
                            Return New list With {
                                .slots = index
                            }
                        End Function)
        Else
            Return data.populates(Of EntityObject)(env) _
                .ToDictionary(Function(a) a.ID, Function(a) CObj(a)) _
                .DoCall(Function(index)
                            Return New list With {
                                .slots = index
                            }
                        End Function)
        End If
    End Function

    ''' <summary>
    ''' Read dataframe
    ''' </summary>
    ''' <param name="file">the csv file</param>
    ''' <param name="mode"></param>
    ''' <returns></returns>
    <ExportAPI("read.dataframe")>
    Public Function readDataSet(file$,
                                Optional mode As DataModes = DataModes.numeric,
                                Optional toRObj As Boolean = False,
                                Optional silent As Boolean = True,
                                Optional strict As Boolean = False,
                                Optional env As Environment = Nothing) As Object

        Dim dataframe As New Rdataframe

        If Not file.FileExists Then
            Dim msg = {$"the given data table file is not exists on your file system!", $"file: {file}"}

            If strict Then
                Return Internal.debug.stop(msg, env)
            Else
                Call env.AddMessage(msg, MSG_TYPES.WRN)
            End If
        End If

        Select Case mode
            Case DataModes.numeric
                Dim data = DataSet.LoadDataSet(file, silent:=silent).ToArray

                If toRObj Then
                    Dim allKeys$() = data.PropertyNames

                    dataframe.rownames = data.Select(Function(r) r.ID).ToArray
                    dataframe.columns = allKeys _
                        .ToDictionary(Function(key) key,
                                      Function(key)
                                          Return DirectCast(data.Select(Function(r) r(key)).ToArray, Array)
                                      End Function)
                Else
                    Return data
                End If
            Case DataModes.character
                Dim data = EntityObject.LoadDataSet(file, silent:=silent).ToArray

                If toRObj Then
                    Dim allKeys$() = data.PropertyNames

                    dataframe.rownames = data.Select(Function(r) r.ID).ToArray
                    dataframe.columns = allKeys _
                        .ToDictionary(Function(key) key,
                                      Function(key)
                                          Return DirectCast(data.Select(Function(r) r(key)).ToArray, Array)
                                      End Function)
                Else
                    Return data
                End If
            Case Else
                dataframe = utils.read_csv(file)
        End Select

        Return dataframe
    End Function
End Module
