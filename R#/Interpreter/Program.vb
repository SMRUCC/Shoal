﻿#Region "Microsoft.VisualBasic::17c561d0893680b70a74378948914e0c, R#\Interpreter\Program.vb"

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

    '     Class Program
    ' 
    '         Constructor: (+1 Overloads) Sub New
    ' 
    '         Function: BuildProgram, CreateProgram, EndWithFuncCalls, Execute, ExecuteCodeLine
    '                   GetEnumerator, IEnumerable_GetEnumerator, isException, ToString
    ' 
    '         Sub: configException, printExpressionDebug
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Logging
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.Blocks
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.Closure
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.DataSets
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.Operators
Imports SMRUCC.Rsharp.Interpreter.SyntaxParser
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Interop

Namespace Interpreter

    Public Class Program : Implements IEnumerable(Of Expression)

        Friend execQueue As Expression()
        Friend Rscript As Rscript

        Sub New()
        End Sub

        Public Function EndWithFuncCalls(ParamArray anyFuncs As String()) As Boolean
            Dim last As Expression = execQueue.LastOrDefault

            If last Is Nothing Then
                Return False
            ElseIf Not TypeOf last Is FunctionInvoke Then
                Return False
            End If

            Dim funcName As Expression = DirectCast(last, FunctionInvoke).funcName

            If Not TypeOf funcName Is Literal Then
                Return False
            End If

            Dim strName As String = CStr(DirectCast(funcName, Literal).value)

            Return anyFuncs.Any(Function(a) a = strName)
        End Function

        ''' <summary>
        ''' function/forloop/if/else/elseif/repeat/while, etc...
        ''' </summary>
        ''' <param name="envir"></param>
        ''' <returns></returns>
        Public Function Execute(envir As Environment) As Object
            Dim last As Object = Nothing
            Dim breakLoop As Boolean = False
            Dim debug As Boolean = envir.globalEnvironment.debugMode

            ' The program code loop
            For Each expression As Expression In execQueue
                last = ExecuteCodeLine(expression, envir, breakLoop, debug)

                If breakLoop Then
                    Call configException(envir, last, expression)
                    Exit For
                End If
            Next

            Return last
        End Function

        Private Sub configException(env As Environment, last As Object, expression As Expression)
            If Not last Is Nothing AndAlso Program.isException(last) Then
                Dim err As Message = last

                If err.source Is Nothing Then
                    err.source = expression
                End If

                env.globalEnvironment.lastException = err
            End If
        End Sub

        Private Shared Sub printExpressionDebug(expression As Expression)
            Dim fore As ConsoleColor = Console.ForegroundColor

            Console.ForegroundColor = ConsoleColor.Magenta
            Console.WriteLine(expression.ToString)
            Console.ForegroundColor = fore
        End Sub

        ''' <summary>
        ''' For execute lambda function
        ''' </summary>
        ''' <param name="expression"></param>
        ''' <param name="envir"></param>
        ''' <param name="breakLoop"></param>
        ''' <returns></returns>
        Public Shared Function ExecuteCodeLine(expression As Expression, envir As Environment,
                                               Optional ByRef breakLoop As Boolean = False,
                                               Optional debug As Boolean = False) As Object

            Dim last As Object

            If debug Then
                Call printExpressionDebug(expression)
            End If

            last = expression.Evaluate(envir)

            ' next keyword will break current closure 
            ' and then goto execute next iteration loop
            If TypeOf expression Is ReturnValue Then
                ' return keyword will break the function
                ' current program maybe is a for loop, if closure, etc
                ' so we needs wrap the last value with 
                ' return keyword.
                last = New ReturnValue(New RuntimeValueLiteral(last))
                breakLoop = True
            ElseIf Not last Is Nothing AndAlso last.GetType Is GetType(ReturnValue) Then
                ' the internal closure invoke a returns keyword
                ' so break the current loop
                '
                ' This situation maybe a deep nested closure, example like 
                '
                ' let fun as function() {
                '    for(x in xxx) {
                '       for(y in yyy) {
                '           if (true(x, y)) {
                '              return ooo;
                '           }
                '       }
                '    }
                ' }
                '
                ' Do not break the returns keyword popout chain 
                '
                breakLoop = True
            End If

            If TypeOf expression Is ContinuteFor Then
                breakLoop = True
            ElseIf TypeOf expression Is BreakLoop Then
                breakLoop = True
            End If

            If Not last Is Nothing Then
                If last.GetType Is GetType(Message) Then
                    If DirectCast(last, Message).level = MSG_TYPES.ERR Then
                        ' throw error will break the expression loop
                        breakLoop = True
                        ' populate out this error message to the top stack
                        ' and then print errors
                        Return last
                    ElseIf DirectCast(last, Message).level = MSG_TYPES.DEBUG Then
                    ElseIf DirectCast(last, Message).level = MSG_TYPES.WRN Then
                    Else

                    End If
                ElseIf last.GetType Is GetType(IfBranch.IfPromise) Then
                    envir.ifPromise.Add(last)
                    last = DirectCast(last, IfBranch.IfPromise).Value

                    If envir.ifPromise.Last.Result Then
                        If Not last Is Nothing AndAlso last.GetType Is GetType(ReturnValue) Then
                            breakLoop = True
                        End If
                    End If
                End If
            End If

            Return last
        End Function

        Public Overrides Function ToString() As String
            Return execQueue _
                .Select(Function(exp) exp.ToString & ";") _
                .JoinBy(vbCrLf)
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Shared Function CreateProgram(Rscript As Rscript, Optional debug As Boolean = False, Optional ByRef error$ = Nothing) As Program
            Dim opts As New SyntaxBuilderOptions With {.debug = debug, .source = Rscript}
            Dim exec As Expression() = Rscript _
                .GetExpressions(opts) _
                .ToArray

            If opts.haveSyntaxErr Then
                [error] = opts.error
                Return Nothing
            Else
                Return New Program With {
                    .execQueue = exec,
                    .Rscript = Rscript
                }
            End If
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Shared Function isException(ByRef result As Object, Optional envir As Environment = Nothing, ByRef Optional isDotNETException As Boolean = False) As Boolean
            If result Is Nothing Then
                Return False
            ElseIf result.GetType Is GetType(Message) Then
                Return DirectCast(result, Message).level = MSG_TYPES.ERR
            ElseIf Not envir Is Nothing AndAlso result.GetType.IsInheritsFrom(GetType(Exception)) Then
                isDotNETException = True
                result = Internal.debug.stop(result, envir)

                Return True
            Else
                Return False
            End If
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        <DebuggerStepThrough>
        Public Shared Function BuildProgram(scriptText As String, Optional debug As Boolean = False, Optional ByRef error$ = Nothing) As Program
            Dim script = Rscript.AutoHandleScript(scriptText)
            Dim program As Program = CreateProgram(script, debug, [error])

            Return program
        End Function

        Public Iterator Function GetEnumerator() As IEnumerator(Of Expression) Implements IEnumerable(Of Expression).GetEnumerator
            For Each line As Expression In execQueue
                Yield line
            Next
        End Function

        Private Iterator Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Yield GetEnumerator()
        End Function
    End Class
End Namespace
