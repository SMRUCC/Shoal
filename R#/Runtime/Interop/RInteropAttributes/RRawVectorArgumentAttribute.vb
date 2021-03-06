﻿#Region "Microsoft.VisualBasic::c4340ceeb03df7a31b388ecae7cacc51, R#\Runtime\Interop\RInteropAttributes\RRawVectorArgumentAttribute.vb"

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

    '     Interface IVectorExpressionLiteral
    ' 
    '         Function: ParseVector
    ' 
    '     Class RRawVectorArgumentAttribute
    ' 
    '         Properties: containsLiteral, parser, vector
    ' 
    '         Constructor: (+1 Overloads) Sub New
    '         Function: GetVector
    ' 
    '     Structure DefaultVectorParser
    ' 
    '         Function: ParseVector
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Namespace Runtime.Interop

    Public Interface IVectorExpressionLiteral

        Function ParseVector(default$, schema As Type) As Array

    End Interface

    ''' <summary>
    ''' 表示这个参数是一个数组，环境系统不应该自动调用getFirst取第一个值
    ''' </summary>
    <AttributeUsage(AttributeTargets.Parameter, AllowMultiple:=False, Inherited:=True)>
    Public Class RRawVectorArgumentAttribute : Inherits RInteropAttribute

        ''' <summary>
        ''' The element type of the target vector type
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>
        ''' If this property is not null, then it means the optional argument have 
        ''' a default string expression value which could be parsed as current vector
        ''' type.
        ''' </remarks>
        Public ReadOnly Property vector As Type
        Public ReadOnly Property parser As Type

        Public ReadOnly Property containsLiteral As Boolean
            Get
                Return Not (vector Is Nothing OrElse parser Is Nothing)
            End Get
        End Property

        ''' <summary>
        ''' <paramref name="parser"/>参数的默认值为<see cref="DefaultVectorParser"/>
        ''' </summary>
        ''' <param name="vector">The element type of the target vector type</param>
        ''' <param name="parser">
        ''' <see cref="IVectorExpressionLiteral"/>
        ''' 
        ''' use <see cref="DefaultVectorParser"/> by default.
        ''' </param>
        Sub New(Optional vector As Type = Nothing, Optional parser As Type = Nothing)
            Me.vector = vector
            Me.parser = If(parser, GetType(DefaultVectorParser))
        End Sub

        Public Function GetVector([default] As String) As Array
            Dim literal As IVectorExpressionLiteral = DirectCast(Activator.CreateInstance(parser), IVectorExpressionLiteral)
            Dim vector As Array = literal.ParseVector([default], schema:=Me.vector)

            Return vector
        End Function
    End Class

    ''' <summary>
    ''' 1. 字符串类型默认使用``|``作为分隔符
    ''' 2. 数值类型默认使用``,``作为分隔符
    ''' </summary>
    Public Structure DefaultVectorParser : Implements IVectorExpressionLiteral

        Public Function ParseVector(default$, schema As Type) As Array Implements IVectorExpressionLiteral.ParseVector
            Select Case schema
                Case GetType(String)
                    Return [default]?.Split("|"c)
                Case GetType(Double)
                    Return [default]?.Split(","c) _
                        .Select(AddressOf Val) _
                        .ToArray
                Case GetType(Integer)
                    Return [default]?.Split(","c) _
                        .Select(AddressOf Integer.Parse) _
                        .ToArray
                Case Else
                    Throw New NotImplementedException(schema.FullName)
            End Select
        End Function
    End Structure
End Namespace
