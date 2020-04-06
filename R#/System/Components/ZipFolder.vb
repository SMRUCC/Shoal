﻿Imports System.IO
Imports System.IO.Compression
Imports Microsoft.VisualBasic.ApplicationServices.Zip

Namespace System.Components

    Public Class ZipFolder : Implements IDisposable

        Default Public ReadOnly Property Item(fileName As String) As Stream
            Get
                If allFiles.ContainsKey(fileName) Then
                    Return ZipStreamReader.Decompress(allFiles(fileName))
                Else
                    Return Nothing
                End If
            End Get
        End Property

        ReadOnly zip As ZipArchive
        ReadOnly allFiles As Dictionary(Of String, ZipArchiveEntry)

        ''' <summary>
        ''' populate all files inside internal of this zip file.
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property ls As String()
            Get
                Return allFiles.Keys.ToArray
            End Get
        End Property

        Sub New(zipFile As String)
            zip = New ZipArchive(zipFile.Open(doClear:=False), ZipArchiveMode.Read)
            allFiles = zip.Entries _
                .ToDictionary(Function(file)
                                  Return file.Name.TrimStart("/"c, "\"c)
                              End Function)
        End Sub

        Public Overrides Function ToString() As String
            Return zip.ToString
        End Function

#Region "IDisposable Support"
        Private disposedValue As Boolean ' 要检测冗余调用

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    ' TODO: 释放托管状态(托管对象)。
                    zip.Dispose()
                End If

                ' TODO: 释放未托管资源(未托管对象)并在以下内容中替代 Finalize()。
                ' TODO: 将大型字段设置为 null。
            End If
            disposedValue = True
        End Sub

        ' TODO: 仅当以上 Dispose(disposing As Boolean)拥有用于释放未托管资源的代码时才替代 Finalize()。
        'Protected Overrides Sub Finalize()
        '    ' 请勿更改此代码。将清理代码放入以上 Dispose(disposing As Boolean)中。
        '    Dispose(False)
        '    MyBase.Finalize()
        'End Sub

        ' Visual Basic 添加此代码以正确实现可释放模式。
        Public Sub Dispose() Implements IDisposable.Dispose
            ' 请勿更改此代码。将清理代码放入以上 Dispose(disposing As Boolean)中。
            Dispose(True)
            ' TODO: 如果在以上内容中替代了 Finalize()，则取消注释以下行。
            ' GC.SuppressFinalize(Me)
        End Sub
#End Region

    End Class
End Namespace