﻿Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Language.UnixBash
Imports Microsoft.VisualBasic.Terminal
Imports SMRUCC.Rsharp.Interpreter
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine
Imports SMRUCC.Rsharp.Runtime
Imports RProgram = SMRUCC.Rsharp.Interpreter.Program

Module Terminal

    Public Function RunTerminal() As Integer
        Dim ps1 As New PS1("> ")
        Dim R As New RInterpreter
        Dim exec As Action(Of String) =
            Sub(script)
                Dim program As RProgram = RProgram.BuildProgram(script)
                Dim result = R.Run(program)

                If Not RProgram.isException(result) Then
                    If program.Count = 1 AndAlso program.isSimplePrintCall Then
                        ' do nothing
                    Else
                        Call Internal.base.print(result)
                    End If
                End If
            End Sub

        Call Console.WriteLine("Type 'demo()' for some demos, 'help()' for on-line help, or
'help.start()' for an HTML browser interface to help.
Type 'q()' to quit R.
")
        Call New Shell(ps1, exec).Run()

        Return 0
    End Function

    <Extension>
    Private Function isSimplePrintCall(program As RProgram) As Boolean
        Return TypeOf program.First Is FunctionInvoke AndAlso DirectCast(program.First, FunctionInvoke).funcName = "print"
    End Function
End Module