﻿#Region "Microsoft.VisualBasic::b12ce75d68a04e7b99d279cfa7cf053f, R#\Interpreter\Syntax\SyntaxImplements\StringInterpolationSyntax.vb"

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

'     Module StringInterpolationSyntax
' 
'         Function: StringInterpolation
' 
' 
' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.DataSets
Imports SMRUCC.Rsharp.Language
Imports SMRUCC.Rsharp.Language.TokenIcer
Imports [String] = SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.DataSets.StringInterpolation

Namespace Interpreter.SyntaxParser.SyntaxImplements

    Module StringInterpolationSyntax

        Public Function StringInterpolation(token As Token, opts As SyntaxBuilderOptions) As SyntaxResult
            Dim tokens As Token() = TokenIcer.StringInterpolation.ParseTokens(token.text)
            Dim block As List(Of Token()) = tokens.SplitByTopLevelDelimiter(TokenType.stringLiteral)
            Dim parts As New List(Of Expression)
            Dim syntaxTemp As SyntaxResult

            For Each part As Token() In block
                If part.isLiteral(TokenType.stringLiteral) Then
                    parts += New Literal(part(Scan0).text)
                Else
                    syntaxTemp = part _
                        .Skip(1) _
                        .Take(part.Length - 2) _
                        .DoCall(Function(code)
                                    Return Expression.CreateExpression(code, opts)
                                End Function)

                    If syntaxTemp.isException Then
                        Return syntaxTemp
                    Else
                        parts += syntaxTemp.expression
                    End If
                End If
            Next

            Return New [String](parts)
        End Function
    End Module
End Namespace
