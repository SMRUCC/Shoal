﻿#Region "Microsoft.VisualBasic::dff0c5bdc03d95d4e7687a6635685b86, R#\Interpreter\ExecuteEngine\ExpressionSymbols\DataSet\DeclareNewSymbol.vb"

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

    '     Class DeclareNewSymbol
    ' 
    '         Properties: expressionName, hasInitializeExpression, isTuple, names, stackFrame
    '                     type, value
    ' 
    '         Constructor: (+1 Overloads) Sub New
    '         Function: Evaluate, getParameterView, PushNames, PushTuple, ToString
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Diagnostics
Imports Microsoft.VisualBasic.Serialization.JSON
Imports SMRUCC.Rsharp.Development.Package.File
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Components.Interface
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports any = Microsoft.VisualBasic.Scripting

Namespace Interpreter.ExecuteEngine.ExpressionSymbols.Closure

    Public Class DeclareNewSymbol : Inherits Expression
        Implements IRuntimeTrace

        ''' <summary>
        ''' 对于tuple类型，会存在多个变量
        ''' </summary>
        Public ReadOnly Property names As String()

        Public ReadOnly Property hasInitializeExpression As Boolean = False

        Friend m_value As Expression
        Friend m_type As TypeCodes
        Friend is_readonly As Boolean

        Public Overrides ReadOnly Property expressionName As ExpressionTypes
            Get
                Return ExpressionTypes.SymbolDeclare
            End Get
        End Property

        Public Overrides ReadOnly Property type As TypeCodes
            Get
                Return m_type
            End Get
        End Property

        Public ReadOnly Property isTuple As Boolean
            <MethodImpl(MethodImplOptions.AggressiveInlining)>
            Get
                Return (Not names.IsNullOrEmpty) AndAlso names.Length > 1
            End Get
        End Property

        ''' <summary>
        ''' 初始值
        ''' </summary>
        Public ReadOnly Property value As Expression
            Get
                Return m_value
            End Get
        End Property

        Public ReadOnly Property stackFrame As StackFrame Implements IRuntimeTrace.stackFrame

        Sub New(names$(), value As Expression, type As TypeCodes, [readonly] As Boolean, stackFrame As StackFrame)
            Me.names = names
            Me.m_value = value
            Me.m_type = type
            Me.is_readonly = [readonly]
            Me.stackFrame = stackFrame

            If Not value Is Nothing Then
                hasInitializeExpression = True
            End If
        End Sub

        ''' <summary>
        ''' Push current variable into the target environment ``<paramref name="envir"/>``.
        ''' </summary>
        ''' <param name="envir"></param>
        ''' <returns></returns>
        Public Overrides Function Evaluate(envir As Environment) As Object
            Dim value As Object

            If Me.value Is Nothing Then
                value = Nothing
            Else
                value = Me.value.Evaluate(envir)
            End If

            If Program.isException(value) Then
                Return value
            Else
                If type = TypeCodes.boolean AndAlso TypeOf value Is String Then
                    value = DirectCast(value, String).ParseBoolean
                End If

                Try
                    Dim err As Message = Nothing
                    ' add new symbol into the given environment stack
                    ' and then returns the value result
                    Call PushNames(names, value, type, is_readonly, envir, err:=err)

                    If Not err Is Nothing Then
                        value = err
                    End If
                Catch ex As Exception
                    value = Internal.debug.stop({
                        ex.Message,
                        "symbols: " & names.JoinBy(","),
                        "value: " & If(value Is Nothing, GetType(Void), value.GetType).FullName,
                        "required: " & type.Description,
                        "is_readonly: " & is_readonly
                    }, envir)
                End Try

                Return value
            End If
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="names">The variable names</param>
        ''' <param name="value">.NET runtime object value.</param>
        ''' <param name="type"></param>
        ''' <param name="envir"></param>
        ''' <returns>
        ''' 用于初始化环境对象
        ''' </returns>
        Friend Shared Function PushNames(names$(),
                                         value As Object,
                                         type As TypeCodes,
                                         [readonly] As Boolean,
                                         envir As Environment,
                                         ByRef err As Message) As Environment

            If Not value Is Nothing AndAlso TypeOf value Is invisible Then
                value = DirectCast(value, invisible).value
            End If

            If names.Length = 1 Then
                value = envir.Push(names(Scan0), value, [readonly], type)
            Else
                ' tuple
                value = PushTuple(names, value, type, [readonly], envir)
            End If

            If Program.isException(value) Then
                err = value
            Else
                err = Nothing
            End If

            Return envir
        End Function

        Private Shared Function PushTuple(names$(),
                                          value As Object,
                                          type As TypeCodes,
                                          [readonly] As Boolean,
                                          envir As Environment) As Object
            Dim rtvl As Object

            If Not value Is Nothing AndAlso TypeOf value Is vector Then
                value = DirectCast(value, vector).data
            End If

            If Not value Is Nothing AndAlso value.GetType.IsArray Then
                Dim vector As Array = value

                If vector.Length = 1 Then
                    ' all set with one value
                    For Each name As String In names
                        rtvl = envir.Push(name, value, [readonly])

                        If Program.isException(rtvl) Then
                            Return rtvl
                        End If
                    Next
                ElseIf vector.Length = names.Length Then
                    ' declare one by one
                    For i As Integer = 0 To vector.Length - 1
                        rtvl = envir.Push(names(i), vector.GetValue(i), [readonly])

                        If Program.isException(rtvl) Then
                            Return rtvl
                        End If
                    Next
                Else
                    Return Internal.debug.stop({
                        $"the value length is not equals to the tuple names!",
                        $"tuple names: [{names.Length}] {names.GetJson}",
                        $"value size: {vector.Length}"
                    }, envir)
                End If
            Else
                ' all set with one value
                For Each name As String In names
                    rtvl = envir.Push(name, value, [readonly])

                    If Program.isException(rtvl) Then
                        Return rtvl
                    End If
                Next
            End If

            Return Nothing
        End Function

        Public Overrides Function ToString() As String
            If names.Length > 1 Then
                Return $"Dim [{names.JoinBy(", ")}] As {type.Description} = {any.ToString(value, "NULL")}"
            ElseIf names.Length = 0 Then
                Return "Syntax Error!"
            Else
                Return $"Dim {names(Scan0)} As {type.Description} = {any.ToString(value, "NULL")}"
            End If
        End Function

        Friend Shared Function getParameterView(declares As DeclareNewSymbol) As String
            Dim a$

            If declares.names.Length = 1 Then
                a = declares.names(Scan0)
            Else
                a = declares.names.JoinBy(", ")
                a = $"[{a}]"
            End If

            If declares.hasInitializeExpression Then
                Return $"{a} = {declares.value}"
            Else
                Return a
            End If
        End Function
    End Class
End Namespace
