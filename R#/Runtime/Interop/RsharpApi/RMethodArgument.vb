﻿#Region "Microsoft.VisualBasic::b147b4ad0d8b77b18913a83953a795cf, R#\Runtime\Interop\RsharpApi\RMethodArgument.vb"

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

    '     Class RMethodArgument
    ' 
    '         Properties: [default], isByrefValueParameter, isObjectList, isOptional, isRequireRawVector
    '                     name, type
    ' 
    '         Function: getDefaultValue, getDefaultVector, GetRawVectorElementType, ParseArgument, ToString
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.Reflection
Imports Microsoft.VisualBasic.ComponentModel.Collection.Generic

Namespace Runtime.Interop

    ''' <summary>
    ''' The R# method argument wrapper
    ''' </summary>
    Public Class RMethodArgument : Implements INamedValue

        ''' <summary>
        ''' The argument name
        ''' </summary>
        ''' <returns></returns>
        Public Property name As String Implements INamedValue.Key
        Public Property type As RType
        Public Property [default] As Object
        Public Property isOptional As Boolean
        Public Property isObjectList As Boolean
        Public Property isByrefValueParameter As Boolean

        ''' <summary>
        ''' Do not apply the <see cref="Runtime.getFirst"/> operation
        ''' </summary>
        ''' <returns></returns>
        Public Property isRequireRawVector As Boolean

        Friend rawVectorFlag As RRawVectorArgumentAttribute
        Friend defaultScriptValue As RDefaultValueAttribute

        ''' <summary>
        ''' Get element type of the target raw vector
        ''' </summary>
        ''' <returns></returns>
        Public Function GetRawVectorElementType() As Type
            If Not rawVectorFlag Is Nothing Then
                Return rawVectorFlag.vector
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function ToString() As String
            Return markdown()
        End Function

        Public Shared Function ParseArgument(p As ParameterInfo) As RMethodArgument
            ' System.MissingMethodException: .ctor 
            Dim rawVectorFlag = p.GetCustomAttribute(Of RRawVectorArgumentAttribute)
            Dim [default] = p.GetCustomAttribute(Of RDefaultValueAttribute)
            Dim isObj As Boolean = Not p.GetCustomAttribute(Of RListObjectArgumentAttribute) Is Nothing
            Dim isByref As Boolean = Not p.GetCustomAttribute(Of RByRefValueAssignAttribute) Is Nothing
            Dim hasExpression As Boolean = Not p.GetCustomAttribute(Of RDefaultExpressionAttribute) Is Nothing

            Return New RMethodArgument With {
                .name = p.Name,
                .type = RType.GetRSharpType(p.ParameterType),
                .rawVectorFlag = rawVectorFlag,
                .defaultScriptValue = [default],
                .[default] = getDefaultValue(
                    rawVector:= .rawVectorFlag,
                    defaultScript:= .defaultScriptValue,
                    paramType:=p.ParameterType,
                    hasExpression:=hasExpression,
                    [default]:=p.DefaultValue
                ),
                .isOptional = p.HasDefaultValue,
                .isObjectList = isObj,
                .isRequireRawVector = Not .rawVectorFlag Is Nothing,
                .isByrefValueParameter = isByref
            }
        End Function

        Private Shared Function getDefaultValue(rawVector As RRawVectorArgumentAttribute,
                                                defaultScript As RDefaultValueAttribute,
                                                paramType As Type,
                                                hasExpression As Boolean,
                                                [default] As Object) As Object
            If hasExpression AndAlso TypeOf [default] Is String AndAlso DirectCast([default], String).FirstOrDefault = "~"c Then
                Return RDefaultExpressionAttribute.ParseDefaultExpression(CStr([default]))
            ElseIf rawVector Is Nothing Then
                If Not defaultScript Is Nothing Then
                    Return defaultScript.ParseDefaultValue(paramType)
                Else
                    Return [default]
                End If
            Else
                Return getDefaultVector(rawVector, [default])
            End If
        End Function

        Private Shared Function getDefaultVector(flag As RRawVectorArgumentAttribute, [default] As Object) As Object
            If flag Is Nothing OrElse [default] Is Nothing Then
                Return [default]
            ElseIf Not flag.containsLiteral Then
                Return [default]
            End If

            If [default].GetType Is GetType(String) Then
                ' parser works for string expression only
                Return flag.GetVector(DirectCast([default], String))
            Else
                Return [default]
            End If
        End Function
    End Class
End Namespace
