﻿Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components

Namespace Interpreter.ExecuteEngine

    Public Class Suppress : Inherits Expression

        Public Overrides ReadOnly Property type As TypeCodes
        Public ReadOnly Property expression As Expression

        Sub New(evaluate As Expression)
            expression = evaluate
        End Sub

        Public Overrides Function Evaluate(envir As Environment) As Object
            Dim result As Object = expression.Evaluate(envir)

            If Not result Is Nothing AndAlso result.GetType Is GetType(Message) Then
                envir.GlobalEnvironment.messages.Add(result)
                Return Nothing
            Else
                Return result
            End If
        End Function
    End Class
End Namespace