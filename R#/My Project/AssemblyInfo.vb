﻿Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices

' General Information about an assembly is controlled through the following
' set of attributes. Change these attribute values to modify the information
' associated with an assembly.

' Review the values of the assembly attributes
#If netcore5 = 0 Then


<Assembly: AssemblyTitle("R# language runtime and script interpreter")>
<Assembly: AssemblyDescription("R# language runtime and script interpreter")>
<Assembly: AssemblyCompany("SMRUCC genomics Institute <genomics@SMRUCC.org>")>
<Assembly: AssemblyProduct("GCModeller")>
<Assembly: AssemblyCopyright("Copyright © xie.guigang@gcmodeller.org 2019")>
<Assembly: AssemblyTrademark("R#")>

<Assembly: ComVisible(False)>

'The following GUID is for the ID of the typelib if this project is exposed to COM
<Assembly: Guid("b7cee4b8-0683-4943-84bc-5596073d8c26")>

' Version information for an assembly consists of the following four values:
'
'      Major Version
'      Minor Version
'      Build Number
'      Revision
'
' You can specify all the values or you can default the Build and Revision Numbers
' by using the '*' as shown below:
' <Assembly: AssemblyVersion("1.0.*")>

<Assembly: AssemblyVersion("2.333.*")>
<Assembly: AssemblyFileVersion("2.13.*")>
#End If