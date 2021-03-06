﻿#Region "Microsoft.VisualBasic::a74dfc2d2406f1cb92880ba5708abcb8, R#\System\Package\PackageFile\AssemblyPack.vb"

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

    '     Class AssemblyPack
    ' 
    '         Properties: assembly, directory, framework
    ' 
    '         Function: GenericEnumerator, GetEnumerator, GetFileContents
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports Microsoft.VisualBasic.Linq

Namespace Development.Package.File

    Public Class AssemblyPack : Implements Enumeration(Of String)

        Public Property framework As String
        ''' <summary>
        ''' dll files, apply for md5 checksum calculation
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>
        ''' 程序会将几乎所有的文件都打包进去
        ''' </remarks>
        Public Property assembly As String()
        Public Property directory As String

        Public Function GetFileContents() As String()
            Return directory _
                .ListFiles("*.*") _
                .Where(Function(path)
                           Return Not path.ExtensionSuffix("pdb")
                       End Function) _
                .ToArray
        End Function

        Public Iterator Function GenericEnumerator() As IEnumerator(Of String) Implements Enumeration(Of String).GenericEnumerator
            For Each dll As String In assembly
                Yield dll
            Next
        End Function

        Public Iterator Function GetEnumerator() As IEnumerator Implements Enumeration(Of String).GetEnumerator
            Yield GenericEnumerator()
        End Function
    End Class
End Namespace
