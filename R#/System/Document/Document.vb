﻿Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Diagnostics
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.Utility
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Text.Xml.Models
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.Closure
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.DataSets
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.Operators
Imports SMRUCC.Rsharp.Runtime.Components

Namespace System

    Public Class Document

        Public Property title As String
        Public Property description As String
        Public Property parameters As NamedValue()
        Public Property returns As String
        Public Property author As String()
        Public Property details As String
        Public Property keywords As String()
        Public Property declares As FunctionDeclare
        Public Property examples As String
        Public Property see_also As String

        Public Overrides Function ToString() As String
            Return title
        End Function

        Public Function UnixMan() As UnixManPage
            Dim man As New UnixManPage With {
                .AUTHOR = author.SafeQuery.JoinBy(","),
                .DESCRIPTION = description,
                .DETAILS = details,
                .NAME = declares.name,
                .VALUE = returns,
                .OPTIONS = declares.parameters _
                    .SafeQuery _
                    .Select(Function(arg)
                                Return New NamedValue(Of String)(arg.name, arg.text)
                            End Function) _
                    .ToArray,
                .PROLOG = declares.ToString,
                .EXAMPLES = examples,
                .FILES = declares.sourceMap.ToString,
                .SEE_ALSO = see_also,
                .SYNOPSIS = declares.ToString,
                .index = New Index With {
                    .category = 1,
                    .keyword = keywords.JoinBy(", "),
                    .index = declares.name,
                    .[date] = Now,
                    .title = title
                }
            }

            Return man
        End Function

    End Class

    Public Class FunctionDeclare

        Public Property name As String
        Public Property parameters As NamedValue()
        Public Property sourceMap As StackFrame

        Public Overrides Function ToString() As String
            Return $"{name}({parameters.Select(Function(a) "").JoinBy(", ")})"
        End Function

        Public Shared Function GetArgument(arg As DeclareNewSymbol) As NamedValue
            Dim name As String = arg.names.JoinBy(", ")
            Dim val As String = Nothing

            If arg.hasInitializeExpression Then
                val = valueText(arg.m_value)
            End If

            Return New NamedValue With {
                .name = name,
                .text = val
            }
        End Function

        Private Shared Function valueText(expr As Expression) As String
            Select Case expr.GetType
                Case GetType(Literal)
                    Dim literal As Literal = expr

                    Select Case literal.type
                        Case TypeCodes.boolean : Return literal.Evaluate(Nothing).ToString.ToUpper
                        Case TypeCodes.double, TypeCodes.integer : Return literal.Evaluate(Nothing).ToString
                        Case TypeCodes.string : Return literal.Evaluate(Nothing).ToString
                        Case Else
                            Throw New InvalidCastException
                    End Select
                Case GetType(VectorLiteral)

                    Return $"[{DirectCast(expr, VectorLiteral).Select(AddressOf valueText).JoinBy(", ")}]"

                Case GetType(SymbolReference)

                    Return DirectCast(expr, SymbolReference).symbol

                Case GetType(FunctionInvoke)

                    Dim funCall As FunctionInvoke = DirectCast(expr, FunctionInvoke)
                    Dim args = funCall.parameters.Select(AddressOf valueText).ToArray

                    Return $"{valueText(funCall.funcName)}({args.JoinBy(", ")})"

                Case GetType(ValueAssign)

                    Dim assign As ValueAssign = DirectCast(expr, ValueAssign)
                    Dim symbol As String() = assign.targetSymbols.Select(AddressOf valueText).ToArray
                    Dim symbolText As String

                    If symbol.Length = 1 Then
                        symbolText = symbol(Scan0)
                    Else
                        symbolText = $"[{symbol.JoinBy(", ")}]"
                    End If

                    Return $"{symbolText} = {valueText(assign.value)}"

                Case Else
                    Throw New NotImplementedException(expr.ToString)
            End Select
        End Function
    End Class
End Namespace