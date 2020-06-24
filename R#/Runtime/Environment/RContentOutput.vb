﻿#Region "Microsoft.VisualBasic::dd122b38a714ae3dd2c1fff68805db50, R#\Runtime\Environment\RContentOutput.vb"

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

'     Enum OutputEnvironments
' 
'         Console, Html, None
' 
'  
' 
' 
' 
'     Class RContentOutput
' 
'         Properties: env, recommendType, stream
' 
'         Constructor: (+1 Overloads) Sub New
'         Sub: Flush, (+4 Overloads) Write, WriteLine, WriteStream
' 
' 
' /********************************************************************************/

#End Region

Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Runtime.Internal.Object

Namespace Runtime

    ''' <summary>
    ''' R# I/O redirect and interface for Rserve http server
    ''' </summary>
    Public Class RContentOutput : Inherits TextWriter

        Public ReadOnly Property env As OutputEnvironments = OutputEnvironments.Console
        Public ReadOnly Property recommendType As String
        Public ReadOnly Property stream As Stream
            Get
                Return stdout.BaseStream
            End Get
        End Property

        Public Overrides ReadOnly Property Encoding As Encoding
            Get
                Return stdout.Encoding
            End Get
        End Property

        Dim stdout As StreamWriter
        Dim logfile As StreamWriter
        Dim split As Boolean

        Sub New(stdout As StreamWriter, env As OutputEnvironments)
            Me.env = env
            Me.stdout = stdout
        End Sub

        Public Sub openSink(logfile As String, Optional split As Boolean = True, Optional append As Boolean = False)
            Me.split = split
            Me.logfile = logfile.OpenWriter(append:=append)
        End Sub

        Public Sub closeSink()
            Call logfile.Flush()
            Call logfile.Close()

            logfile = Nothing
        End Sub

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Overrides Sub Flush()
            Call stdout.Flush()

            If Not logfile Is Nothing Then
                Call logfile.Flush()
            End If
        End Sub

        Public Overrides Sub WriteLine()
            If split Then
                Call stdout.WriteLine()
                Call stdout.Flush()
            End If

            If Not logfile Is Nothing Then
                Call logfile.WriteLine()
            End If

            If _recommendType Is Nothing Then
                _recommendType = "text/html;charset=UTF-8"
            End If
        End Sub

        ''' <summary>
        ''' Writes a string followed by a line terminator to the text string or stream.
        ''' </summary>
        ''' <param name="message">
        ''' The string to write. If value is null, only the line terminator is written.
        ''' </param>
        Public Overrides Sub WriteLine(message As String)
            If split Then
                If message Is Nothing Then
                    Call stdout.WriteLine()
                Else
                    Call stdout.WriteLine(message)
                End If
            End If

            If Not logfile Is Nothing Then
                Call logfile.WriteLine(message Or EmptyString)
            End If

            Call stdout.Flush()

            If _recommendType Is Nothing Then
                _recommendType = "text/html;charset=UTF-8"
            End If
        End Sub

        ''' <summary>
        ''' xml/json/csv, etc...
        ''' </summary>
        ''' <param name="message"></param>
        ''' <param name="content_type$"></param>
        Public Overloads Sub Write(message As String, content_type$)
            If split Then
                Call stdout.Write(message)
                Call stdout.Flush()
            End If

            If Not logfile Is Nothing Then
                Call logfile.Write(message)
            End If

            If _recommendType Is Nothing Then
                _recommendType = content_type
            End If
        End Sub

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Overloads Sub Write(data As dataframe, globalEnv As GlobalEnvironment, Optional content_type$ = "text/csv")
            Call data _
                .GetTable(globalEnv, False, True) _
                .Select(Function(row)
                            Return row.Select(Function(cell) $"""{cell}""").JoinBy(",")
                        End Function) _
                .JoinBy(vbCrLf) _
                .DoCall(Sub(text)
                            Call Write(text, content_type)
                        End Sub)
        End Sub

        Public Overloads Sub Write(data As IEnumerable(Of Byte))
            Dim buffer As Byte() = data.ToArray
            Dim base = stdout.BaseStream

            If split Then
                For Each bit As Byte In buffer
                    Call base.WriteByte(bit)
                Next
            End If

            If Not logfile Is Nothing Then
                base = logfile.BaseStream

                For Each bit As Byte In data
                    Call base.WriteByte(bit)
                Next
            End If

            If _recommendType Is Nothing Then
                _recommendType = "application/octet-stream"
            End If
        End Sub

        Public Overloads Sub Write(image As Image)
            _recommendType = "image/png"

            Using buffer As New MemoryStream
                Call image.Save(buffer, ImageFormat.Png)
                Call Write(buffer.ToArray)
            End Using
        End Sub

        Public Sub WriteStream(writeTo As Action(Of Stream), content_type As String)
            _recommendType = content_type

            Call writeTo(stream)
        End Sub
    End Class
End Namespace
