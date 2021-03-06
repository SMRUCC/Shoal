﻿#Region "Microsoft.VisualBasic::f1660cc74ca874706e5d3fa74ecfa89e, Library\R.math\models\lmCall.vb"

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

    ' Class lmCall
    ' 
    '     Properties: data, formula, lm, name, variables
    '                 weights
    ' 
    '     Constructor: (+1 Overloads) Sub New
    '     Function: CreateFormulaCall, Predicts, ToString
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Data.Bootstrapping
Imports Microsoft.VisualBasic.Data.Bootstrapping.Multivariate
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.DataSets
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.Operators

Public Class lmCall

    Public Property lm As IFitted
    Public Property formula As FormulaExpression
    Public Property name As String
    Public Property variables As String()
    Public Property data As String
    Public Property weights As String

    Sub New(name As String, variables As String())
        Me.name = name
        Me.variables = variables
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Function Predicts(x As Double()) As Double
        Return lm.GetY(x)
    End Function

    Public Overrides Function ToString() As String
        If TypeOf lm Is WeightedFit Then
            Dim formula = DirectCast(DirectCast(lm, WeightedFit).Polynomial, Polynomial)

            Return $"
Call:
lm(formula = {formula.ToString(variables, "G3")}, data = <{data}>, weights = {weights})

Coefficients:
(Intercept)            {variables(Scan0)}  
  {formula.Factors(Scan0).ToString("G4")}    {formula.Factors(1).ToString("G4")}

"
        ElseIf TypeOf lm Is FitResult Then
            Dim formula = DirectCast(DirectCast(lm, FitResult).Polynomial, Polynomial)

            Return $"
Call:
lm(formula = {formula.ToString(variables, "G3")}, data = <{data}>)

Coefficients:
(Intercept)            {variables(Scan0)}    
  {formula.Factors(Scan0).ToString("G4")}    {formula.Factors(1).ToString("G4")}  

"
        Else
            Dim formula = DirectCast(DirectCast(lm, MLRFit).Polynomial, MultivariatePolynomial)

            Return $"
Call:
lm(formula = {formula.ToString(variables, "G3")}, data = <{data}>)

Coefficients:
(Intercept)            {variables(Scan0)}    
  {formula.Factors(Scan0).ToString("G4")}    {formula.Factors(1).ToString("G4")}  

"
        End If
    End Function

    Public Function CreateFormulaCall() As Expression
        If TypeOf lm Is MLRFit Then
            Dim poly = DirectCast(lm.Polynomial, MultivariatePolynomial)
            Dim exp As Expression = New Literal(poly.Factors(Scan0))

            For i As Integer = 1 To poly.Factors.Length - 1
                exp = New BinaryExpression(
                    exp,
                    New BinaryExpression(
                        New Literal(poly.Factors(i)),
                        New SymbolReference(variables(i - 1)),
                        "*"),
                    "+")
            Next

            Return exp
        Else
            Dim linear = DirectCast(lm.Polynomial, Polynomial)
            Dim exp As Expression = New Literal(linear.Factors(Scan0))
            Dim singleSymbol As String = variables(Scan0)

            For i As Integer = 1 To linear.Factors.Length - 1
                exp = New BinaryExpression(
                    exp,
                    New BinaryExpression(
                        New Literal(linear.Factors(i)),
                        New BinaryExpression(
                            New SymbolReference(singleSymbol),
                            New Literal(i),
                            "^"),
                        "*"),
                    "+")
            Next

            Return exp
        End If
    End Function
End Class
