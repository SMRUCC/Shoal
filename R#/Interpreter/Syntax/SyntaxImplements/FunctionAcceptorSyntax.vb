﻿#Region "Microsoft.VisualBasic::3c8a355c40864b1ffcbdb3902de11072, R#\Interpreter\Syntax\SyntaxImplements\FunctionAcceptorSyntax.vb"

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

    '     Module FunctionAcceptorSyntax
    ' 
    '         Function: FunctionAcceptorInvoke
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.Closure
Imports SMRUCC.Rsharp.Language
Imports SMRUCC.Rsharp.Language.TokenIcer
Imports SMRUCC.Rsharp.Runtime.Components

Namespace Interpreter.SyntaxParser.SyntaxImplements

    Module FunctionAcceptorSyntax

        Public Function FunctionAcceptorInvoke(tokens As Token(), opts As SyntaxBuilderOptions) As SyntaxResult
            Dim code = tokens.SplitByTopLevelDelimiter(TokenType.close, tokenText:=")")
            Dim funcInvoke As SyntaxResult = code(Scan0).JoinIterates(code(1)).DoCall(Function(fi) Expression.CreateExpression(fi, opts))

            If funcInvoke.isException Then
                Return funcInvoke
            End If

            Dim Rscript As Rscript = opts.source
            Dim invoke As FunctionInvoke = funcInvoke.expression
            Dim firstParameterBody As Expression() = code(2) _
                .Skip(1) _
                .Take(code(2).Length - 2) _
                .ToArray _
                .GetExpressions(Rscript, Nothing, opts) _
                .ToArray

            If opts.haveSyntaxErr Then
                Return New SyntaxResult(New SyntaxErrorException(opts.error), opts.debug)
            End If

            Dim parameterClosure As New ClosureExpression(firstParameterBody)
            Dim args As New List(Of Expression)

            args.Add(parameterClosure)
            args.AddRange(invoke.parameters)

            invoke.parameters = args.ToArray

            Return New SyntaxResult(invoke)
        End Function
    End Module
End Namespace
