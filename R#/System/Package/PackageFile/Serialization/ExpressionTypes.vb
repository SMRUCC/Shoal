﻿#Region "Microsoft.VisualBasic::69e7d11ccd9d7df62569ab5060aa9828, R#\System\Package\PackageFile\Serialization\ExpressionTypes.vb"

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

    '     Enum ExpressionTypes
    ' 
    '         Binary, FunctionCall, FunctionDeclare, Literal, Require
    '         SymbolDeclare, SymbolReference, UnaryNot, VectorLiteral
    ' 
    '  
    ' 
    ' 
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Namespace System.Package.File

    Public Enum ExpressionTypes As Integer
        Literal
        VectorLiteral
        StringInterpolation
        FunctionCall
        FunctionDeclare
        SymbolDeclare
        SymbolReference
        Require
        UnaryNot
        Binary
    End Enum
End Namespace