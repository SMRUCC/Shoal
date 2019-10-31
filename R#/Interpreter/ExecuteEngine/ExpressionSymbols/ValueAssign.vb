﻿Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Language.TokenIcer
Imports SMRUCC.Rsharp.Runtime

Namespace Interpreter.ExecuteEngine

    Public Class ValueAssign : Inherits Expression

        Public Overrides ReadOnly Property type As TypeCodes

        ''' <summary>
        ''' 可能是对tuple做赋值
        ''' 所以应该是多个变量名称
        ''' </summary>
        Friend targetSymbols As String()
        Friend isByRef As Boolean
        Friend value As Expression

        Sub New(tokens As List(Of Token()))
            targetSymbols = DeclareNewVariable.getNames(tokens(Scan0))
            isByRef = tokens(Scan0)(Scan0).text = "="
            value = tokens.Skip(2).AsList.DoCall(AddressOf Expression.ParseExpression)
        End Sub

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Overrides Function Evaluate(envir As Environment) As Object
            Return DoValueAssign(envir, value.Evaluate(envir))
        End Function

        Public Function DoValueAssign(envir As Environment, value As Object) As Object
            If value.GetType Is GetType(IfBranch.IfPromise) Then
                DirectCast(value, IfBranch.IfPromise).assignTo = Me
                Return value
            End If

            Dim message As Message

            If targetSymbols.Length = 1 Then
                message = assignSymbol(envir, targetSymbols(Scan0), value)
            Else
                ' assign tuples
                message = assignTuples(envir, value)
            End If

            If message Is Nothing Then
                Return value
            Else
                Return message
            End If
        End Function

        Public Overrides Function ToString() As String
            If targetSymbols.Length = 1 Then
                Return $"{targetSymbols(0)} <- {value.ToString}"
            Else
                Return $"[{targetSymbols.JoinBy(", ")}] <- {value.ToString}"
            End If
        End Function

        Private Function assignTuples(envir As Environment, value As Object) As Message
            If value.GetType.IsInheritsFrom(GetType(Array)) Then
                Dim array As Array = value
                Dim message As New Value(Of Message)

                If array.Length = 1 Then
                    ' all assign the same value result
                    For Each name As String In targetSymbols
                        If Not (message = assignSymbol(envir, name, value)) Is Nothing Then
                            Return message
                        End If
                    Next
                ElseIf array.Length = targetSymbols.Length Then
                    ' one by one
                    For i As Integer = 0 To array.Length - 1
                        If Not (message = assignSymbol(envir, targetSymbols(i), array.GetValue(i))) Is Nothing Then
                            Return message
                        End If
                    Next
                Else
                    ' 数量不对
                    Throw New InvalidCastException
                End If
            Else
                Throw New NotImplementedException
            End If

            Return Nothing
        End Function

        Private Function assignSymbol(envir As Environment, symbolName$, value As Object) As Message
            Dim target As Variable = envir.FindSymbol(symbolName)

            If target Is Nothing Then
                Return Message.SymbolNotFound(envir, symbolName, TypeCodes.generic)
            End If

            If isByRef Then
                target.value = value
            Else
                If value.GetType.IsInheritsFrom(GetType(Array)) Then
                    target.value = DirectCast(value, Array).Clone
                Else
                    target.value = value
                End If
            End If

            Return Nothing
        End Function
    End Class
End Namespace