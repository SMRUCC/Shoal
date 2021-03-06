﻿#Region "Microsoft.VisualBasic::b62e619f044c20f044d3e2715099010f, R#\Interpreter\Syntax\SyntaxImplements\FormulaExpressionSyntax.vb"

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

    '     Module FormulaExpressionSyntax
    ' 
    '         Function: GetExpressionLiteral, RunParse
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Diagnostics
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.DataSets
Imports SMRUCC.Rsharp.Language.TokenIcer

Namespace Interpreter.SyntaxParser.SyntaxImplements

    Module FormulaExpressionSyntax

        Public Function RunParse(code As Token()(), opts As SyntaxBuilderOptions) As SyntaxResult
            Dim y As String = code(Scan0)(Scan0).text
            Dim expression As SyntaxResult = SyntaxResult.CreateExpression(code.Skip(2).IteratesALL, opts)

            If expression.isException Then
                Return expression
            End If

            Return New FormulaExpression(y, expression.expression)
        End Function

        Public Function GetExpressionLiteral(code As Token()(), opts As SyntaxBuilderOptions) As SyntaxResult
            Dim literalTokens As Token() = code.Skip(1).IteratesALL.ToArray
            Dim stackFrame As StackFrame = opts.GetStackTrace(code(Scan0)(Scan0), $"[expression_literal] {literalTokens.Select(Function(a) a.text).JoinBy("")}")
            Dim literal As SyntaxResult = SyntaxResult.CreateExpression(literalTokens, opts)

            If literal.isException Then
                Return literal
            Else
                Return New ExpressionLiteral(literal.expression, stackFrame)
            End If
        End Function
    End Module
End Namespace
