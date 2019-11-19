﻿#Region "Microsoft.VisualBasic::dd3b4e1f921f1c56e4e4a0c280aa845d, R#\Interpreter\ExecuteEngine\ExpressionSymbols\DeclareLambdaFunction.vb"

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

    '     Class DeclareLambdaFunction
    ' 
    '         Properties: name, type
    ' 
    '         Constructor: (+1 Overloads) Sub New
    '         Function: Evaluate, Invoke, ToString
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Language.TokenIcer
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Components.Interface

Namespace Interpreter.ExecuteEngine

    ''' <summary>
    ''' 只允许简单的表达式出现在这里
    ''' 并且参数也只允许出现一个
    ''' </summary>
    Public Class DeclareLambdaFunction : Inherits Expression
        Implements RFunction

        Public Overrides ReadOnly Property type As TypeCodes
            Get
                Return TypeCodes.closure
            End Get
        End Property

        Public ReadOnly Property name As String Implements RFunction.name

        Dim parameter As DeclareNewVariable
        Dim closure As ClosureExpression

        ''' <summary>
        ''' 只允许拥有一个参数，并且只允许出现一行代码
        ''' </summary>
        ''' <param name="tokens"></param>
        Sub New(tokens As List(Of Token()))
            With tokens.ToArray
                name = .IteratesALL _
                       .Select(Function(t) t.text) _
                       .JoinBy(" ") _
                       .DoCall(Function(exp)
                                   Return "[lambda: " & exp & "]"
                               End Function)
                parameter = New DeclareNewVariable(tokens(Scan0))
                closure = ClosureExpression.ParseExpressionTree(.Skip(2).IteratesALL)
            End With
        End Sub

        ''' <summary>
        ''' 返回的是一个<see cref="RFunction"/>
        ''' </summary>
        ''' <param name="envir"></param>
        ''' <returns></returns>
        Public Overrides Function Evaluate(envir As Environment) As Object
            Return Me
        End Function

        Public Function Invoke(parent As Environment, arguments() As InvokeParameter) As Object Implements RFunction.Invoke
            Using envir As New Environment(parent, name)
                Return DeclareNewVariable _
                    .PushNames(names:=parameter.names,
                               value:=arguments(Scan0).Evaluate(envir),
                               type:=TypeCodes.generic,
                               envir:=envir
                    ) _
                    .DoCall(AddressOf closure.Evaluate)
            End Using
        End Function

        Public Overrides Function ToString() As String
            Return name
        End Function
    End Class
End Namespace
