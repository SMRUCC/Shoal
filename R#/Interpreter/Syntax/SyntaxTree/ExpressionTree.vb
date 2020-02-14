﻿#Region "Microsoft.VisualBasic::d290de33c6afb02c305b44e3eb37b84c, R#\Interpreter\Syntax\SyntaxTree\ExpressionTree.vb"

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

    '     Module ExpressionTree
    ' 
    '         Function: CreateTree, ParseExpressionTree, simpleSequence
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine
Imports SMRUCC.Rsharp.Language
Imports SMRUCC.Rsharp.Language.TokenIcer

Namespace Interpreter.SyntaxParser

    Module ExpressionTree

        <Extension>
        Public Function CreateTree(tokens As Token(), opts As SyntaxBuilderOptions) As SyntaxResult
            Dim blocks As List(Of Token()) = tokens.SplitByTopLevelDelimiter(TokenType.comma)

            If blocks = 1 Then
                Dim expression As SyntaxResult = blocks(Scan0).simpleSequence(opts)

                If Not expression Is Nothing Then
                    Return expression
                Else
                    ' 是一个复杂的表达式
                    Return blocks(Scan0).ParseExpressionTree(opts)
                End If
            Else
                Return New SyntaxResult(New NotImplementedException, opts.debug)
            End If
        End Function

        <Extension>
        Private Function simpleSequence(tokens As Token(), opts As SyntaxBuilderOptions) As SyntaxResult
            Dim blocks = tokens.SplitByTopLevelDelimiter(TokenType.sequence, includeKeyword:=True)

            If blocks = 3 Then
                Return SyntaxImplements.SequenceLiteral(blocks(Scan0), blocks(2), Nothing, opts)
            ElseIf blocks = 5 Then
                Return SyntaxImplements.SequenceLiteral(blocks(Scan0), blocks(2), blocks.ElementAtOrDefault(4), opts)
            Else
                Return Nothing
            End If
        End Function

        <Extension>
        Private Function ParseExpressionTree(tokens As Token(), opts As SyntaxBuilderOptions) As SyntaxResult
            Dim blocks As List(Of Token())

            If tokens.Length = 1 Then
                If tokens(Scan0).name = TokenType.stringInterpolation Then
                    Return SyntaxImplements.StringInterpolation(tokens(Scan0), opts)
                ElseIf tokens(Scan0).name = TokenType.cliShellInvoke Then
                    Return SyntaxImplements.CommandLine(tokens(Scan0), opts)
                ElseIf tokens(Scan0) = (TokenType.operator, "$") Then
                    Return New SymbolReference("$")
                Else
                    blocks = New List(Of Token()) From {tokens}
                End If
            ElseIf tokens.Length = 2 AndAlso tokens(Scan0).name = TokenType.iif Then
                Return SyntaxImplements.CommandLineArgument(tokens, opts)
            Else
                blocks = tokens.SplitByTopLevelDelimiter(TokenType.operator)
            End If

            If blocks = 1 Then
                ' 简单的表达式
                If tokens.isFunctionInvoke Then
                    Return SyntaxImplements.FunctionInvoke(tokens, opts)
                ElseIf tokens.isSimpleSymbolIndexer Then
                    Return SyntaxImplements.SymbolIndexer(tokens, opts)
                ElseIf tokens(Scan0).name = TokenType.open Then
                    Dim openSymbol = tokens(Scan0).text

                    If openSymbol = "[" Then
                        Return SyntaxImplements.VectorLiteral(tokens, opts)
                    ElseIf openSymbol = "(" Then
                        ' (xxxx)
                        ' (xxxx)[xx]
                        ' (function(){...})(...)
                        Dim splitTokens = tokens.SplitByTopLevelDelimiter(TokenType.close)

                        If splitTokens = 2 Then
                            '  0     1
                            ' (xxxx |) is a single expression
                            ' 是一个表达式
                            Return splitTokens(Scan0) _
                                .Skip(1) _
                                .SplitByTopLevelDelimiter(TokenType.operator) _
                                .ParseBinaryExpression(opts)
                        ElseIf splitTokens = 4 Then
                            If splitTokens.Last.Length = 1 AndAlso splitTokens.Last()(Scan0) = (TokenType.close, "]") Then
                                ' 0      1    2     3
                                ' (xxxx |) | [xxx | ]
                                ' symbol indexer
                                Dim symbolExpression As Token() = splitTokens(Scan0).Skip(1).ToArray
                                Dim indexerExpression As Token() = splitTokens(2) _
                                    .JoinIterates(splitTokens.Last) _
                                    .ToArray

                                Return SyntaxImplements.SymbolIndexer(symbolExpression, indexerExpression, opts)
                            Else
                                ' 可能是一个匿名函数的调用
                                ' (function(...){})(...)
                                If splitTokens(1).Length = 1 AndAlso splitTokens(1)(Scan0).text = ")" AndAlso
                                   splitTokens(2).Length = 1 AndAlso splitTokens(2)(Scan0).text = "(" AndAlso
                                   splitTokens.Last.Length = 1 AndAlso splitTokens.Last(Scan0).text = ")" Then

                                    Return SyntaxImplements.AnonymousFunctionInvoke(
                                        anonymous:=splitTokens(Scan0).Skip(1).ToArray,
                                        invoke:=splitTokens.Skip(2).IteratesALL.ToArray,
                                        opts:=opts
                                    )
                                Else
                                    Return New SyntaxResult(New SyntaxErrorException, opts.debug)
                                End If
                            End If
                        Else
                            Throw New NotImplementedException
                        End If
                    ElseIf openSymbol = "{" Then
                        ' 是一个可以产生值的closure
                        Return SyntaxImplements.ClosureExpression(tokens, opts)
                    End If
                ElseIf tokens(Scan0).name = TokenType.stringInterpolation Then
                    Return SyntaxImplements.StringInterpolation(tokens(Scan0), opts)
                Else
                    Dim indexer As SyntaxResult = tokens.parseComplexSymbolIndexer(opts)

                    If Not indexer Is Nothing Then
                        Return indexer
                    End If
                End If
            Else
                Return ParseBinaryExpression(blocks, opts)
            End If

            Return New SyntaxResult(New NotImplementedException, opts.debug)
        End Function
    End Module
End Namespace
