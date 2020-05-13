﻿#Region "Microsoft.VisualBasic::4ff27dfedbe3fb262a277f0a12e1c5a8, R#\Interpreter\Syntax\SyntaxImplements\ForLoopSyntax.vb"

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

    '     Module ForLoopSyntax
    ' 
    '         Function: ForLoop, ParseLoopBody
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Diagnostics
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.Blocks
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.Closure
Imports SMRUCC.Rsharp.Language
Imports SMRUCC.Rsharp.Language.TokenIcer

Namespace Interpreter.SyntaxParser.SyntaxImplements

    Module ForLoopSyntax

        Public Function ForLoop(tokens As IEnumerable(Of Token), opts As SyntaxBuilderOptions) As SyntaxResult
            Dim blocks As List(Of Token()) = tokens.SplitByTopLevelDelimiter(TokenType.close)
            Dim [loop] As List(Of Token()) = blocks(Scan0) _
                .Skip(1) _
                .SplitByTopLevelDelimiter(TokenType.keyword)
            Dim vars As Token() = [loop](Scan0)
            Dim variables$()

            If vars.Length = 1 Then
                variables = {vars(Scan0).text}
            Else
                variables = vars _
                    .Skip(1) _
                    .Take(vars.Length - 2) _
                    .Where(Function(x) Not x.name = TokenType.comma) _
                    .Select(Function(x) x.text) _
                    .ToArray
            End If

            Dim sequence As SyntaxResult = [loop] _
                .Skip(2) _
                .IteratesALL _
                .DoCall(Function(code)
                            Return Expression.CreateExpression(code, opts)
                        End Function)

            If sequence.isException Then
                Return sequence
            End If

            Dim parallel As Boolean = False
            Dim loopBody As SyntaxResult = ParseLoopBody(
                tokens:=blocks(2),
                isParallel:=parallel,
                opts:=opts
            )

            If loopBody.isException Then
                Return loopBody
            End If

            Dim stackframe As New StackFrame With {
                .File = opts.source.fileName,
                .Line = blocks.First(Scan0).span.line,
                .Method = New Method With {
                    .Method = "forloop",
                    .[Module] = "forloop",
                    .[Namespace] = "SMRUCC/R#"
                }
            }
            Dim body As New DeclareNewFunction(
                body:=loopBody.expression,
                funcName:="forloop_internal",
                params:={},
                stackframe:=stackframe
            )
            Dim [for] As New ForLoop(variables, sequence.expression, body, parallel, stackframe)

            Return New SyntaxResult([for])
        End Function

        Private Function ParseLoopBody(tokens As Token(), ByRef isParallel As Boolean, opts As SyntaxBuilderOptions) As SyntaxResult
            If tokens(Scan0) = (TokenType.open, "{") Then
                Return tokens _
                    .Skip(1) _
                    .DoCall(Function(code)
                                Return ClosureExpressionSyntax.ClosureExpression(code, opts)
                            End Function)
            ElseIf tokens(Scan0) = (TokenType.operator, "%") AndAlso
                   tokens(1) = (TokenType.identifier, "dopar") AndAlso
                   tokens(2) = (TokenType.operator, "%") Then

                ' for(...) %dopar% {...}
                isParallel = True

                Return tokens _
                    .Skip(4) _
                    .DoCall(Function(code)
                                Return ClosureExpressionSyntax.ClosureExpression(code, opts)
                            End Function)
            Else
                Throw New SyntaxErrorException
            End If
        End Function
    End Module
End Namespace
