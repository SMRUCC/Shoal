﻿#Region "Microsoft.VisualBasic::a9f5777262e24d739a9421ee380a4f06, R#\Interpreter\ExecuteEngine\ExpressionSymbols\DataSet\SymbolReference.vb"

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

'     Class SymbolReference
' 
'         Properties: symbol, type
' 
'         Constructor: (+2 Overloads) Sub New
'         Function: Evaluate, GetReferenceObject, ToString
' 
' 
' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.Language
Imports SMRUCC.Rsharp.Language.TokenIcer
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Interop
Imports SMRUCC.Rsharp.System.Package.File
Imports REnv = SMRUCC.Rsharp.Runtime.Internal

Namespace Interpreter.ExecuteEngine.ExpressionSymbols.DataSets

    ''' <summary>
    ''' 符号引用
    ''' </summary>
    Public Class SymbolReference : Inherits Expression

        Public Overrides ReadOnly Property type As TypeCodes
            Get
                Return TypeCodes.ref
            End Get
        End Property

        Public Overrides ReadOnly Property expressionName As ExpressionTypes
            Get
                Return ExpressionTypes.SymbolReference
            End Get
        End Property

        ''' <summary>
        ''' 目标变量名
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property symbol As String

        Friend Sub New(symbol As Token)
            Me.symbol = symbol.text
        End Sub

        Sub New(symbol As String)
            Me.symbol = symbol
        End Sub

        Public Overrides Function Evaluate(envir As Environment) As Object
            Return GetReferenceObject(symbol, envir)
        End Function

        Public Shared Function GetReferenceObject(symbolName As String, env As Environment, Optional [inherits] As Boolean = True) As Object
            Dim symbol As [Variant](Of Symbol, RMethodInfo) = env.FindSymbol(symbolName, [inherits])

            If symbol Is Nothing OrElse Not symbol.HasValue Then
                symbol = REnv.invoke.getFunction(symbolName)
            End If
            If symbol Is Nothing OrElse Not symbol.HasValue Then
                Return Message.SymbolNotFound(env, symbolName, TypeCodes.generic)
            ElseIf symbol Like GetType(Symbol) Then
                Return symbol.VA.value
            Else
                Return symbol.Value
            End If
        End Function

        Public Overrides Function ToString() As String
            Return $"&{symbol}"
        End Function
    End Class
End Namespace
