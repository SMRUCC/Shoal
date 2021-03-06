﻿#Region "Microsoft.VisualBasic::583f63867eaac86c4510072ff5e76ef0, R#\Interpreter\Syntax\SyntaxImplements\SymbolIndexer.vb"

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

    '     Module SymbolIndexerSyntax
    ' 
    '         Function: (+2 Overloads) SymbolIndexer
    ' 
    '         Sub: parseDataframeIndex, parseIndex
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.DataSets
Imports SMRUCC.Rsharp.Language
Imports SMRUCC.Rsharp.Language.TokenIcer

Namespace Interpreter.SyntaxParser.SyntaxImplements

    Module SymbolIndexerSyntax

        ''' <summary>
        ''' Simple indexer
        ''' </summary>
        ''' <param name="tokens">
        ''' ``a[x]``
        ''' </param>
        Public Function SymbolIndexer(tokens As Token(), opts As SyntaxBuilderOptions) As SyntaxResult
            Dim symbol As SyntaxResult = {tokens(Scan0)}.DoCall(Function(code) Expression.CreateExpression(code, opts))
            Dim indexType As SymbolIndexers
            Dim index As SyntaxResult = Nothing

            If symbol.isException Then
                Return symbol
            End If

            tokens = tokens _
                .Skip(2) _
                .Take(tokens.Length - 3) _
                .ToArray

            If tokens.isStackOf("[", "]") Then
                ' 脱掉最外侧的[]
                ' 可能为 x[["X"]] 取值
                ' 也可能为 x[ [a,b,c] ] 取子集
                tokens = tokens _
                    .Skip(1) _
                    .Take(tokens.Length - 2) _
                    .ToArray

                index = opts.UsingVectorBuilder(Function(opt) Expression.CreateExpression(tokens, opt))

                If index.isException Then
                    Return index
                ElseIf TypeOf index.expression Is VectorLiteral Then
                    indexType = SymbolIndexers.vectorIndex
                Else
                    indexType = SymbolIndexers.nameIndex
                End If
            Else
                Call tokens.parseIndex(index, indexType, opts)
            End If

            If index.isException Then
                Return index
            End If

            Return New SymbolIndexer(symbol.expression, index.expression, indexType)
        End Function

        ''' <summary>
        ''' Complex indexer
        ''' 
        ''' ```
        ''' func(...)[x]
        ''' func(...)[[x]]
        ''' ```
        ''' </summary>
        ''' <param name="ref"></param>
        ''' <param name="indexer"></param>
        Public Function SymbolIndexer(ref As Token(), indexer As Token(), opts As SyntaxBuilderOptions) As SyntaxResult
            Dim symbol As SyntaxResult = Expression.CreateExpression(ref, opts)
            Dim index As SyntaxResult = Nothing
            Dim indexType As SymbolIndexers

            If symbol.isException Then
                Return symbol
            End If

            indexer = indexer _
                .Skip(1) _
                .Take(indexer.Length - 2) _
                .ToArray

            Call indexer.parseIndex(index, indexType, opts)

            If index.isException Then
                Return index
            End If

            Return New SymbolIndexer(symbol.expression, index.expression, indexType)
        End Function

        <Extension>
        Private Sub parseIndex(tokens As Token(), ByRef index As SyntaxResult, ByRef indexType As SymbolIndexers, opts As SyntaxBuilderOptions)
            Dim blocks As List(Of Token()) = tokens.SplitByTopLevelDelimiter(TokenType.comma, False)

            If blocks > 1 Then
                ' dataframe indexer
                blocks.parseDataframeIndex(index, indexType, opts)
            Else
                If tokens.isStackOf("[", "]") Then
                    blocks = tokens.Skip(1).Take(tokens.Length - 2).SplitByTopLevelDelimiter(TokenType.comma, False)

                    If blocks = 1 Then
                        ' list get value by name
                        indexType = SymbolIndexers.nameIndex
                        tokens = blocks(Scan0)
                    Else
                        ' list subset by names
                        ' list[[a,b,c,d]]
                        indexType = SymbolIndexers.vectorIndex
                    End If
                Else
                    ' vector indexer
                    ' list subset by names
                    indexType = SymbolIndexers.vectorIndex
                End If

                index = Expression.CreateExpression(tokens, opts)
            End If
        End Sub

        ''' <summary>
        ''' dataframe indexer
        ''' </summary>
        ''' <param name="blocks"></param>
        ''' <param name="index"></param>
        ''' <param name="indexType"></param>
        <Extension>
        Private Sub parseDataframeIndex(blocks As List(Of Token()), ByRef index As SyntaxResult, ByRef indexType As SymbolIndexers, opts As SyntaxBuilderOptions)
            If blocks(0).isComma Then
                ' x[, a] by columns
                indexType = SymbolIndexers.dataframeColumns
                index = Expression.CreateExpression(blocks.Skip(1).IteratesALL, opts)
            ElseIf blocks = 2 AndAlso blocks(1).isComma Then
                ' x[a, ] by row
                indexType = SymbolIndexers.dataframeRows
                index = Expression.CreateExpression(blocks(Scan0), opts)
            Else
                Dim elements As New List(Of Expression)

                ' x[a,b] by range
                indexType = SymbolIndexers.dataframeRanges

                For Each result As SyntaxResult In blocks _
                    .Where(Function(t) Not t.isComma) _
                    .Select(Function(tokens)
                                Return Expression.CreateExpression(tokens, opts)
                            End Function)

                    If result.isException Then
                        index = result
                        Return
                    Else
                        elements.Add(result.expression)
                    End If
                Next

                index = New VectorLiteral(elements.ToArray)
            End If
        End Sub
    End Module
End Namespace
