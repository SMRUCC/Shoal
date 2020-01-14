﻿#Region "Microsoft.VisualBasic::d3941e9270f99879ba0fae56c1241fd7, R#\Runtime\Interop\RMethodArgument.vb"

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
'         Function: ParseArgument, ToString
' 
' 
' /********************************************************************************/

#End Region

Imports System.Reflection
Imports Microsoft.VisualBasic.ComponentModel.Collection.Generic
Imports Microsoft.VisualBasic.Serialization
Imports SMRUCC.Rsharp.Runtime.Internal.ConsolePrinter

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
            Dim defaultValue As String = "``<NULL>``"

            If [default] Is Nothing Then
                defaultValue = "``<NULL>``"
            ElseIf isOptional Then
                If [default].GetType Is GetType(String) Then
                    defaultValue = $"""{[default]}"""
                ElseIf type.raw.IsEnum Then
                    defaultValue = enumPrinter.defaultValueToString([default], type)
                ElseIf type.raw.IsArray Then
                    defaultValue = JSON.GetObjectJson([default].GetType, [default], indent:=False)
                Else
                    defaultValue = [default].ToString.ToUpper
                End If
            End If

            If isObjectList Then
                Return "..."
            End If

            If type.isEnvironment Then
                Return $"[``<Environment>``]"
            End If

            If isOptional Then
                Return $"``{name}`` as {type} = {defaultValue}"
            Else
                Return $"``{name}`` as {type}"
            End If
        End Function

        Public Shared Function ParseArgument(p As ParameterInfo) As RMethodArgument
            Return New RMethodArgument With {
                .name = p.Name,
                .type = RType.GetRSharpType(p.ParameterType),
                .rawVectorFlag = p.GetCustomAttribute(Of RRawVectorArgumentAttribute),
                .[default] = getDefaultVector(.rawVectorFlag, p.DefaultValue),
                .isOptional = p.HasDefaultValue,
                .isObjectList = Not p.GetCustomAttribute(Of RListObjectArgumentAttribute) Is Nothing,
                .isRequireRawVector = Not .rawVectorFlag Is Nothing,
                .isByrefValueParameter = Not p.GetCustomAttribute(Of RByRefValueAssignAttribute) Is Nothing
            }
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
