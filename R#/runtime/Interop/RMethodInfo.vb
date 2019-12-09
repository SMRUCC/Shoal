﻿#Region "Microsoft.VisualBasic::66e97ccea84125adc9aef9588317a71e, R#\Runtime\Interop\RMethodInfo.vb"

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

    '     Class RMethodInfo
    ' 
    '         Properties: name, parameters, returns
    ' 
    '         Constructor: (+3 Overloads) Sub New
    '         Function: createNormalArguments, createObjectListArguments, GetPrintContent, getValue, Invoke
    '                   missingParameter, parseParameters, ToString
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Components.Interface
Imports SMRUCC.Rsharp.Runtime.Internal

Namespace Runtime.Interop

    ''' <summary>
    ''' Use for R# package method
    ''' </summary>
    Public Class RMethodInfo : Implements RFunction, RPrint

        ''' <summary>
        ''' The function name
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property name As String Implements RFunction.name
        Public ReadOnly Property returns As RType
        Public ReadOnly Property parameters As RMethodArgument()

        ReadOnly api As [Variant](Of MethodInvoke, [Delegate])

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="name"></param>
        ''' <param name="closure">
        ''' Runtime generated .NET method
        ''' </param>
        Sub New(name$, closure As [Delegate])
            Me.name = name
            Me.api = closure
            Me.returns = New RType(closure.Method.ReturnType)
            Me.parameters = closure.Method.DoCall(AddressOf parseParameters)
        End Sub

        ''' <summary>
        ''' Static method
        ''' </summary>
        ''' <param name="api"></param>
        Sub New(api As NamedValue(Of MethodInfo))
            Call Me.New(api.Name, api.Value, Nothing)
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="name"></param>
        ''' <param name="closure"><see cref="MethodInfo"/> from parsing .NET dll module file.</param>
        ''' <param name="target"></param>
        Sub New(name$, closure As MethodInfo, target As Object)
            Me.name = name
            Me.api = New MethodInvoke With {.method = closure, .target = target}
            Me.returns = New RType(closure.ReturnType)
            Me.parameters = closure.DoCall(AddressOf parseParameters)
        End Sub

        ''' <summary>
        ''' Gets the <see cref="MethodInfo"/> represented by the delegate.
        ''' </summary>
        ''' <returns></returns>
        Public Function GetRawDeclares() As MethodInfo
            If api Like GetType(MethodInvoke) Then
                Return api.TryCast(Of MethodInvoke).method
            Else
                Return api.TryCast(Of [Delegate]).Method
            End If
        End Function

        Public Function GetPrintContent() As String Implements RPrint.GetPrintContent
            Return $"let {name} as function({parameters.JoinBy(", ")}) {{
    return call R#.interop_{name}(...);
}}"
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Private Shared Function parseParameters(method As MethodInfo) As RMethodArgument()
            Return method _
                .GetParameters _
                .Select(AddressOf RMethodArgument.ParseArgument) _
                .ToArray
        End Function

        Public Function Invoke(envir As Environment, params As InvokeParameter()) As Object Implements RFunction.Invoke
            Dim parameters As Object()

            If Me.parameters.Any(Function(a) a.isObjectList) Then
                parameters = createObjectListArguments(envir, params).ToArray
            Else
                parameters = InvokeParameter _
                    .CreateArguments(envir, params) _
                    .DoCall(Function(args)
                                Return createNormalArguments(envir, args)
                            End Function) _
                    .ToArray
            End If

            For Each arg In parameters
                If Not arg Is Nothing AndAlso arg.GetType Is GetType(Message) Then
                    Return arg
                End If
            Next

            Dim result As Object

            If api Like GetType(MethodInvoke) Then
                result = api.TryCast(Of MethodInvoke).Invoke(parameters)
            Else
                result = api.VB.Method.Invoke(Nothing, parameters.ToArray)
            End If

            Return result
        End Function

        Private Function createObjectListArguments(envir As Environment, params As InvokeParameter()) As IEnumerable(Of Object)
            Dim parameterVals As Object() = New Object(Me.parameters.Length - 1) {}
            Dim declareArguments = Me.parameters.ToDictionary(Function(a) a.name)
            Dim declareNameIndex As Index(Of String) = Me.parameters.Keys.Indexing
            Dim listObject As New List(Of InvokeParameter)
            Dim i As Integer
            Dim sequenceIndex As Integer = Scan0

            For Each arg As InvokeParameter In params
                If declareArguments.ContainsKey(arg.name) OrElse Not arg.isSymbolAssign Then
                    i = declareNameIndex(arg.name)

                    If i > -1 Then
                        parameterVals(i) = getValue(declareArguments(arg.name), arg.Evaluate(envir), trace:=name)
                        declareArguments.Remove(arg.name)
                    Else
                        parameterVals(sequenceIndex) = getValue(declareArguments.First.Value, arg.Evaluate(envir), trace:=name)
                        declareArguments.Remove(declareArguments.First.Key)
                    End If
                Else
                    listObject.Add(arg)
                End If

                sequenceIndex += 1
            Next

            ' get index of list argument
            i = Me.parameters _
                .First(Function(a) a.isObjectList).name _
                .DoCall(Function(a)
                            Call declareArguments.Remove(a)
                            Return declareNameIndex.IndexOf(a)
                        End Function)
            parameterVals(i) = listObject.ToArray

            If declareArguments.Count > 0 Then
                Dim envirArgument As RMethodArgument = declareArguments _
                    .Values _
                    .Where(Function(a)
                               Return a.type.raw Is GetType(Environment)
                           End Function) _
                    .FirstOrDefault

                If Not envirArgument Is Nothing Then
                    i = declareNameIndex(envirArgument.name)
                    parameterVals(i) = envir
                    declareArguments.Remove(envirArgument.name)
                End If
            End If

            If declareArguments.Count > 0 Then
                Return {missingParameter(declareArguments.Values.First, envir, name)}
            Else
                Return parameterVals
            End If
        End Function

        Private Iterator Function createNormalArguments(envir As Environment, arguments As Dictionary(Of String, Object)) As IEnumerable(Of Object)
            Dim arg As RMethodArgument
            Dim keys As String() = arguments.Keys.ToArray
            Dim nameKey As String
            Dim apiTrace As String = name

            For Each value As Object In arguments.Values
                If Not value Is Nothing AndAlso value.GetType Is GetType(Message) Then
                    Yield value
                    Return
                End If
            Next

            For i As Integer = 0 To Me.parameters.Length - 1
                arg = Me.parameters(i)

                If arguments.ContainsKey(arg.name) Then
                    Yield getValue(arg, arguments(arg.name), apiTrace)
                ElseIf i >= arguments.Count Then
                    ' default value
                    If arg.type.raw Is GetType(Environment) Then
                        Yield envir
                    ElseIf Not arg.isOptional Then
                        Yield missingParameter(arg, envir, name)
                    Else
                        Yield arg.default
                    End If
                Else
                    nameKey = $"${i}"

                    If arguments.ContainsKey(nameKey) Then
                        Yield getValue(arg, arguments(nameKey), apiTrace)
                    Else
                        Yield getValue(arg, arguments(keys(i)), apiTrace)
                    End If
                End If
            Next
        End Function

        Private Shared Function missingParameter(arg As RMethodArgument, envir As Environment, name$) As Object
            Dim messages$() = {
                $"Missing parameter value for '{arg.name}'!",
                $"parameter: {arg.name}",
                $"type: {arg.type.raw.FullName}",
                $"function: {name}",
                $"environment: {envir.ToString}"
            }

            Return Internal.stop(messages, envir)
        End Function

        Private Shared Function getValue(arg As RMethodArgument, value As Object, trace$) As Object
            If arg.type.isArray Then
                value = CObj(Runtime.asVector(value, arg.type.GetRawElementType))
            ElseIf arg.type.isCollection Then
                ' ienumerable
                value = value
            ElseIf Not arg.isRequireRawVector Then
                value = Runtime.getFirst(value)
            End If

            Try
                Return RConversion.CTypeDynamic(value, arg.type.raw)
            Catch ex As Exception
                Throw New InvalidCastException("Api: " & trace, ex)
            End Try
        End Function

        Public Overrides Function ToString() As String
            Return $"Dim {name} As {api.ToString}"
        End Function
    End Class
End Namespace
