﻿Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ApplicationServices.Terminal
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Language
Imports SMRUCC.Rsharp.Runtime

Namespace Interpreter

    Public Class RInterpreter

        ''' <summary>
        ''' Global runtime environment.(全局环境)
        ''' </summary>
        Public ReadOnly Property globalEnvir As New Environment
        Public ReadOnly Property warnings As New List(Of Message)

        Default Public ReadOnly Property GetValue(name As String) As Object
            Get
                Return globalEnvir(name).value
            End Get
        End Property

        Public Const LastVariableName$ = "$"

        Sub New()
            Call globalEnvir.Push(LastVariableName, Nothing, TypeCodes.generic)
        End Sub

        Public Sub PrintMemory(Optional dev As TextWriter = Nothing)
            Dim table$()() = globalEnvir _
                .Select(Function(v)
                            Dim value$ = Variable.GetValueViewString(v)

                            Return {
                                v.name,
                                v.typeCode.ToString,
                                v.typeof.FullName,
                                $"[{v.length}] {value}"
                            }
                        End Function) _
                .ToArray

            With dev Or Console.Out.AsDefault
                Call .DoCall(Sub(device)
                                 Call table.PrintTable(
                                    dev:=device,
                                    leftMargin:=3,
                                    title:={"name", "mode", "typeof", "value"}
                                 )
                             End Sub)
            End With
        End Sub

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Sub Add(name$, value As Object, Optional type As TypeCodes = TypeCodes.generic)
            Call globalEnvir.Push(name, value, type)
        End Sub

        Public Sub Add(name$, closure As [Delegate])
            Throw New NotImplementedException
        End Sub

        Public Function Invoke(funcName$, ParamArray args As Object()) As Object
            Dim symbol = globalEnvir.FindSymbol(funcName)

            If symbol Is Nothing Then
                Throw New EntryPointNotFoundException($"No object named '{funcName}' could be found in global environment!")
            ElseIf symbol.typeCode <> TypeCodes.closure Then
                Throw New InvalidProgramException($"Object '{funcName}' is not a function!")
            End If

            Throw New NotImplementedException
        End Function

        ''' <summary>
        ''' Run R# script program from text data.
        ''' </summary>
        ''' <param name="script$"></param>
        ''' <returns></returns>
        ''' 
        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Public Function Evaluate(script As String) As Object
            Dim result As Object = Code.ParseScript(script).RunProgram(globalEnvir)
            Dim last As Variable = globalEnvir(LastVariableName)

            ' set last variable in current environment
            last.value = result

            Return result
        End Function

        Public Shared ReadOnly Property Rsharp As New RInterpreter

        Public Shared Function Evaluate(script$, ParamArray args As NamedValue(Of Object)()) As Object
            SyncLock Rsharp
                With Rsharp
                    If Not args.IsNullOrEmpty Then
                        Dim name$
                        Dim value As Object

                        For Each var As NamedValue(Of Object) In args
                            name = var.Name
                            value = var.Value

                            Call .globalEnvir.Push(name, value, NameOf(TypeCodes.generic))
                        Next
                    End If

                    Return .Evaluate(script)
                End With
            End SyncLock
        End Function
    End Class
End Namespace