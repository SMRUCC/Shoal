﻿#Region "Microsoft.VisualBasic::e7f9ec75dd709b20290cc755d5f6f74b, R#\Interpreter\ExecuteEngine\ExpressionSymbols\Operators\BinaryOrExpression.vb"

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

    '     Class BinaryOrExpression
    ' 
    '         Properties: expressionName, type
    ' 
    '         Constructor: (+1 Overloads) Sub New
    '         Function: Evaluate, ToString
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports SMRUCC.Rsharp.Development.Package.File
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Interop
Imports REnv = SMRUCC.Rsharp.Runtime

Namespace Interpreter.ExecuteEngine.ExpressionSymbols.Operators

    Public Class BinaryOrExpression : Inherits Expression

        Public Overrides ReadOnly Property type As TypeCodes

        Public Overrides ReadOnly Property expressionName As ExpressionTypes
            Get
                Return ExpressionTypes.Binary
            End Get
        End Property

        Friend ReadOnly left, right As Expression

        Sub New(a As Expression, b As Expression)
            left = a
            right = b
        End Sub

        Public Overrides Function Evaluate(envir As Environment) As Object
            ' 20191216
            ' fix for
            ' value || stop(xxxx)
            Dim a As Object = left.Evaluate(envir)
            Dim ta As Type = RType.TypeOf(a)

            If ta Like RType.logicals Then
                ' boolean = a || b
                Dim b As Object = right.Evaluate(envir)

                If Program.isException(b) Then
                    Return b
                Else
                    Return REnv.Core _
                        .BinaryCoreInternal(Of Boolean, Boolean, Boolean)(
                            x:=Core.asLogical(a),
                            y:=Core.asLogical(b),
                            [do]:=Function(x, y) x OrElse y
                        ) _
                        .ToArray
                End If
            Else
                ' let arg as string = ?"--opt" || default;
                If Internal.Invokes.base.isEmpty(a) Then
                    Return right.Evaluate(envir)
                Else
                    Return a
                End If
            End If
        End Function

        Public Overrides Function ToString() As String
            Return $"({left} || {right})"
        End Function
    End Class
End Namespace
