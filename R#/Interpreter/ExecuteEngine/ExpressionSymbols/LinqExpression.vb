﻿Imports Microsoft.VisualBasic.Emit.Marshal
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Language
Imports SMRUCC.Rsharp.Language.TokenIcer
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components

Namespace Interpreter.ExecuteEngine

    ' from x in seq let y = ... where test select projection order by ... asceding distinct 

    Public Class LinqExpression : Inherits Expression

        Public Overrides ReadOnly Property type As TypeCodes
            Get
                Return sequence.type
            End Get
        End Property

        ''' <summary>
        ''' in
        ''' </summary>
        Dim sequence As Expression
        ''' <summary>
        ''' from
        ''' </summary>
        Dim variable As DeclareNewVariable
        Dim declares As New List(Of DeclareNewVariable)
        Dim program As ClosureExpression
        ''' <summary>
        ''' select
        ''' </summary>
        Dim projection As Expression

        Dim output As ClosureExpression

        Sub New(tokens As List(Of Token()))
            Dim variables As New List(Of String)
            Dim i As Integer = 0

            For i = 1 To tokens.Count - 1
                If tokens(i).isIdentifier Then
                    variables.Add(tokens(i)(Scan0).text)
                ElseIf tokens(i).isKeyword("in") Then
                    sequence = Expression.CreateExpression(tokens(i + 1))
                    Exit For
                End If
            Next

            If sequence Is Nothing Then
                Throw New SyntaxErrorException
            Else
                i += 2
                variable = New DeclareNewVariable With {
                    .names = variables.ToArray,
                    .hasInitializeExpression = False,
                    .value = Nothing
                }
            End If

            tokens = tokens _
                .Skip(i) _
                .IteratesALL _
                .SplitByTopLevelDelimiter(TokenType.keyword)

            Call New Pointer(Of Token())(tokens).DoCall(AddressOf doParseLINQProgram)
        End Sub

        Shared ReadOnly linqKeywordDelimiters As String() = {"where", "distinct", "select", "order", "group", "let"}

        Private Sub doParseLINQProgram(p As Pointer(Of Token()))
            Dim buffer As New List(Of Token())
            Dim token As Token()
            Dim program As New List(Of Expression)
            Dim expression As Expression
            Dim output As New List(Of Expression)

            Do While Not p.EndRead
                buffer *= 0
                token = ++p

                If token.isKeyword Then
                    Select Case token(Scan0).text
                        Case "let"
                            buffer += token

                            Do While Not p.EndRead AndAlso Not p.Current.isOneOfKeywords(linqKeywordDelimiters)
                                buffer += ++p
                            Loop

                            declares += buffer _
                                .IteratesALL _
                                .SplitByTopLevelDelimiter(TokenType.operator, True) _
                                .DoCall(Function(blocks)
                                            Return New DeclareNewVariable(blocks)
                                        End Function)
                            program += New ValueAssign(declares.Last.names, declares.Last.value)
                        Case "where"
                            Do While Not p.EndRead AndAlso Not p.Current.isOneOfKeywords(linqKeywordDelimiters)
                                buffer += ++p
                            Loop

                            expression = buffer _
                                .IteratesALL _
                                .DoCall(AddressOf Expression.CreateExpression)
                            program += New IfBranch(expression, {New ReturnValue(Literal.NULL)})
                        Case "distinct"
                            output += New FunctionInvoke("unique", New SymbolReference("$"))
                        Case "order"
                            ' order by xxx asc
                            Do While Not p.EndRead AndAlso Not p.Current.isOneOfKeywords(linqKeywordDelimiters)
                                buffer += ++p
                            Loop

                            token = buffer.IteratesALL.ToArray

                            If Not token(Scan0).isKeyword("by") Then
                                Throw New SyntaxErrorException
                            End If

                            ' skip first by keyword
                            expression = token _
                                .Skip(1) _
                                .Take(token.Length - 2) _
                                .DoCall(AddressOf Expression.CreateExpression)
                            output += New FunctionInvoke("sort", expression, New Literal(token.Last.isKeyword("descending")))
                        Case "select"
                            If Not projection Is Nothing Then
                                Throw New SyntaxErrorException("Only allows one project function!")
                            End If

                            Do While Not p.EndRead AndAlso Not p.Current.isOneOfKeywords(linqKeywordDelimiters)
                                buffer += ++p
                            Loop

                            projection = Expression.CreateExpression(buffer.IteratesALL)
                        Case "group"
                            Do While Not p.EndRead AndAlso Not p.Current.isOneOfKeywords(linqKeywordDelimiters)
                                buffer += ++p
                            Loop

                            Throw New NotImplementedException
                        Case Else
                            Throw New SyntaxErrorException
                    End Select
                End If
            Loop

            Me.program = program.ToArray
            Me.output = output.ToArray
        End Sub

        Public Overrides Function Evaluate(envir As Environment) As Object


            Throw New NotImplementedException
        End Function
    End Class
End Namespace