﻿#Region "Microsoft.VisualBasic::4992689094ddc549d6431ddc24d2de17, R#\System\Package\PackageFile\Serialization\Writer.vb"

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

    '     Class Writer
    ' 
    '         Properties: RBinary, RCallFunction, RClosure, Relse, RExpr
    '                     Rfor, RFunction, Rif, RImports, RLiteral
    '                     RString, RSymbol, RSymbolAssign, RSymbolIndex, RSymbolRef
    '                     RUnary, RVector
    ' 
    '         Constructor: (+1 Overloads) Sub New
    ' 
    '         Function: (+2 Overloads) GetBuffer, GetSymbols, ReadSourceMap, readZEROBlock, Write
    ' 
    '         Sub: (+2 Overloads) Dispose
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.IO
Imports System.Text
Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Diagnostics
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.SecurityString
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.Blocks
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.Closure
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.DataSets
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols.Operators
Imports SMRUCC.Rsharp.Development.Package.File.Expressions

Namespace Development.Package.File

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <remarks>
    ''' expression: [ExpressionTypes, i32][dataSize, i32][TypeCodes, byte][expressionData, bytes]
    '''              4                     4              1                ...
    ''' </remarks>
    Public Class Writer : Implements IDisposable

        Dim buffer As BinaryWriter
        Dim disposedValue As Boolean
        Dim sourceMaps As New List(Of StackFrame)

        Public Const Magic As String = "SMRUCC/R#"

        Shared ReadOnly magicBytes As Byte() = Encoding.ASCII.GetBytes(Magic)

        Public ReadOnly Property RSymbol As RSymbol
        Public ReadOnly Property RSymbolRef As RSymbolReference
        Public ReadOnly Property RSymbolIndex As RSymbolIndex
        Public ReadOnly Property RSymbolAssign As RSymbolAssign
        Public ReadOnly Property RLiteral As RLiteral
        Public ReadOnly Property RBinary As RBinary
        Public ReadOnly Property RCallFunction As RCallFunction
        Public ReadOnly Property RFunction As RFunction
        Public ReadOnly Property RImports As RRequire
        Public ReadOnly Property RUnary As RUnary
        Public ReadOnly Property RVector As RVector
        Public ReadOnly Property RString As RStringInterpolation
        Public ReadOnly Property RExpr As RExprLiteral
        Public ReadOnly Property RClosure As RClosure

#Region "keywords"
        Public ReadOnly Property Rfor As RFor
        Public ReadOnly Property Rif As RIf
        Public ReadOnly Property Relse As RElse
#End Region

        Sub New(buffer As Stream)
            Me.buffer = New BinaryWriter(buffer)

            Call Me.buffer.Write(magicBytes)
            Call Me.buffer.Flush()

            Me.RSymbol = New RSymbol(Me)
            Me.RLiteral = New RLiteral(Me)
            Me.RBinary = New RBinary(Me)
            Me.RCallFunction = New RCallFunction(Me)
            Me.RImports = New RRequire(Me)
            Me.RUnary = New RUnary(Me)
            Me.RVector = New RVector(Me)
            Me.RFunction = New RFunction(Me)
            Me.RSymbolRef = New RSymbolReference(Me)
            Me.RString = New RStringInterpolation(Me)
            Me.RSymbolIndex = New RSymbolIndex(Me)
            Me.RSymbolAssign = New RSymbolAssign(Me)
            Me.Rfor = New RFor(Me)
            Me.Rif = New RIf(Me)
            Me.Relse = New RElse(Me)
            Me.RExpr = New RExprLiteral(Me)
            Me.RClosure = New RClosure(Me)
        End Sub

        Public Function GetBuffer(x As Expression) As Byte()
            Select Case x.GetType
                Case GetType(DeclareNewSymbol) : Return RSymbol.GetBuffer(x)
                Case GetType(DeclareNewFunction),
                     GetType(DeclareLambdaFunction),
                     GetType(FormulaExpression),
                     GetType(UsingClosure)

                    Return RFunction.GetBuffer(x)

                Case GetType(Literal), GetType(Regexp) : Return RLiteral.GetBuffer(x)
                Case GetType(StringInterpolation) : Return RString.GetBuffer(x)
                Case GetType(BinaryOrExpression),
                     GetType(BinaryBetweenExpression),
                     GetType(BinaryInExpression),
                     GetType(AppendOperator),
                     GetType(BinaryExpression)

                    Return RBinary.GetBuffer(x)

                Case GetType(FunctionInvoke),
                     GetType(ByRefFunctionCall),
                     GetType(IIfExpression),
                     GetType(CreateObject),
                     GetType(SequenceLiteral)

                    Return RCallFunction.GetBuffer(x)

                Case GetType(Require), GetType([Imports]) : Return RImports.GetBuffer(x)
                Case GetType(UnaryNot) : Return RUnary.GetBuffer(x)
                Case GetType(VectorLiteral) : Return RVector.GetBuffer(x)
                Case GetType(SymbolReference) : Return RSymbolRef.GetBuffer(x)
                Case GetType(SymbolIndexer) : Return RSymbolIndex.GetBuffer(x)
                Case GetType(ValueAssign) : Return RSymbolAssign.GetBuffer(x)

                Case GetType(ForLoop) : Return Rfor.GetBuffer(x)
                Case GetType(IfBranch) : Return Rif.GetBuffer(x)
                Case GetType(ElseBranch) : Return Relse.GetBuffer(x)
                Case GetType(ExpressionLiteral) : Return RExpr.GetBuffer(x)

                Case GetType(ClosureExpression) : Return RClosure.GetBuffer(x)

                Case Else
                    Throw New NotImplementedException(x.GetType.FullName)
            End Select
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="x"></param>
        ''' <returns>
        ''' 函数返回表达式的长度
        ''' </returns>
        Public Function Write(x As Expression) As String
            Dim buffer As Byte() = GetBuffer(x)
            Dim md5 As New Md5HashProvider

            Call Me.buffer.Write(buffer)

            Return md5.GetMd5Hash(buffer)
        End Function

        ''' <summary>
        ''' 这个函数会将传递进来的sourcemap对象加入到缓存之中
        ''' </summary>
        ''' <param name="sourceMap"></param>
        ''' <returns></returns>
        Public Function GetBuffer(sourceMap As StackFrame) As Byte()
            Using ms As New MemoryStream, outfile As New BinaryWriter(ms)
                Call outfile.Write(0)
                Call outfile.Write(Encoding.ASCII.GetBytes(sourceMap.File))
                Call outfile.Write(CByte(0))
                Call outfile.Write(Encoding.ASCII.GetBytes(sourceMap.Line))
                Call outfile.Write(CByte(0))
                Call outfile.Write(Encoding.ASCII.GetBytes(sourceMap.Method.Namespace))
                Call outfile.Write(CByte(0))
                Call outfile.Write(Encoding.ASCII.GetBytes(sourceMap.Method.Module))
                Call outfile.Write(CByte(0))
                Call outfile.Write(Encoding.ASCII.GetBytes(sourceMap.Method.Method))

                Call outfile.Flush()
                Call ms.Seek(Scan0, SeekOrigin.Begin)
                Call outfile.Write(CInt(ms.Length - 4))
                Call outfile.Flush()
                Call sourceMaps.Add(sourceMap)

                Return ms.ToArray
            End Using
        End Function

        Friend Shared Iterator Function readZEROBlock(bin As BinaryReader) As IEnumerable(Of Byte)
            Dim [byte] As Value(Of Byte) = 0

            Do While ([byte] = bin.ReadByte) <> 0
                Yield [byte].Value
            Loop
        End Function

        Public Shared Function ReadSourceMap(bin As BinaryReader, desc As DESCRIPTION) As StackFrame
            Dim length As Integer = bin.ReadInt32
            Dim bytes As Byte() = bin.ReadBytes(length)
            Dim strings As String() = bytes _
                .Split(Function(b) b = 0, DelimiterLocation.NotIncludes) _
                .Select(AddressOf Encoding.ASCII.GetString) _
                .ToArray

            Return New StackFrame With {
                .File = strings(0),
                .Line = strings(1),
                .Method = New Method With {
                    .[Namespace] = desc.Package,
                    .[Module] = strings(3),
                    .[Method] = strings(4)
                }
            }
        End Function

        Public Function GetSymbols() As IEnumerable(Of StackFrame)
            Return sourceMaps.AsEnumerable
        End Function

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    ' TODO: dispose managed state (managed objects)
                    Call buffer.Flush()
                    Call buffer.Close()
                End If

                ' TODO: free unmanaged resources (unmanaged objects) and override finalizer
                ' TODO: set large fields to null
                disposedValue = True
            End If
        End Sub

        ' ' TODO: override finalizer only if 'Dispose(disposing As Boolean)' has code to free unmanaged resources
        ' Protected Overrides Sub Finalize()
        '     ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
        '     Dispose(disposing:=False)
        '     MyBase.Finalize()
        ' End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
            Dispose(disposing:=True)
            GC.SuppressFinalize(Me)
        End Sub
    End Class
End Namespace
