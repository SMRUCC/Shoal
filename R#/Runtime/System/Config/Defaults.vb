﻿#Region "Microsoft.VisualBasic::20932fe5b6b2e851e17338b8ebae43d7, R#\Runtime\System\Config\Defaults.vb"

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

    '     Module Defaults
    ' 
    '         Properties: HTTPUserAgent
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Namespace Runtime.Components.Configuration

    Module Defaults

        Public ReadOnly Property HTTPUserAgent As String = $"SMRUCC/R# {App.Version}; R (3.4.4 x86_64-w64-mingw32 x86_64 mingw32)"

    End Module
End Namespace
