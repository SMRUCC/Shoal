﻿Imports System.Runtime.CompilerServices
Imports SMRUCC.Rsharp.Language.TokenIcer
Imports SMRUCC.Rsharp.Runtime

Namespace Interpreter.ExecuteEngine

    ''' <summary>
    ''' 其实就是一段拥有自己的<see cref="Environment"/>的没有名称的匿名函数
    ''' </summary>
    Public Class ClosureExpression : Inherits Expression

        Public Overrides ReadOnly Property type As TypeCodes
            Get
                Return TypeCodes.closure
            End Get
        End Property

        Dim program As Program

        Sub New(tokens As IEnumerable(Of Token))
            program = Program.CreateProgram(tokens)
        End Sub

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Overrides Function Evaluate(envir As Environment) As Object
            Return program.Execute(envir)
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Shared Function ParseExpressionTree(tokens As IEnumerable(Of Token)) As ClosureExpression
            Return New ClosureExpression(tokens)
        End Function
    End Class
End Namespace