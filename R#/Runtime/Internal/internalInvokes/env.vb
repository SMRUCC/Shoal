﻿Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Components.Interface
Imports SMRUCC.Rsharp.Runtime.Interop

Namespace Runtime.Internal.Invokes

    Module env

        ''' <summary>
        ''' # Return the Value of a Named Object
        ''' 
        ''' Search by name for an object (get) or zero or more objects (mget).
        ''' </summary>
        ''' <param name="x">For get, an object name (given as a character string).
        ''' For mget, a character vector of object names.</param>
        ''' <param name="envir">where to look for the object (see ‘Details’); if omitted search as if the name of the object appeared unquoted in an expression.</param>
        ''' <param name="inherits">
        ''' should the enclosing frames of the environment be searched?
        ''' </param>
        ''' <returns></returns>
        <ExportAPI("get")>
        Public Function [get](x As Object, envir As Environment, Optional [inherits] As Boolean = True) As Object
            Dim name As String = Runtime.asVector(Of Object)(x) _
                .DoCall(Function(o)
                            Return Scripting.ToString(Runtime.getFirst(o), null:=Nothing)
                        End Function)

            If name.StringEmpty Then
                Return Internal.stop("NULL value provided for object name!", envir)
            End If

            Dim symbol As Variable = envir.FindSymbol(name, [inherits])

            If symbol Is Nothing Then
                Return Message.SymbolNotFound(envir, name, TypeCodes.generic)
            Else
                Return symbol.value
            End If
        End Function

        <ExportAPI("globalenv")>
        <DebuggerStepThrough>
        Private Function globalenv(env As Environment) As Object
            Return env.globalEnvironment
        End Function

        <ExportAPI("environment")>
        <DebuggerStepThrough>
        Private Function environment(env As Environment) As Object
            Return env
        End Function

        <ExportAPI("ls")>
        Private Function ls(env As Environment) As Object
            Return env.variables.Keys.ToArray
        End Function

        <ExportAPI("objects")>
        Private Function objects(env As Environment) As Object
            Return env.variables.Keys.ToArray
        End Function

        ''' <summary>
        ''' # Execute a Function Call
        ''' 
        ''' ``do.call`` constructs and executes a function call from a name or 
        ''' a function and a list of arguments to be passed to it.
        ''' </summary>
        ''' <param name="what"></param>
        ''' <param name="calls">
        ''' either a function or a non-empty character string naming the function 
        ''' to be called.
        ''' </param>
        ''' <param name="args">
        ''' a list of arguments to the function call. The names attribute of 
        ''' args gives the argument names.
        ''' </param>
        ''' <param name="envir">
        ''' an environment within which to evaluate the call. This will be most 
        ''' useful if what is a character string and the arguments are symbols 
        ''' or quoted expressions.
        ''' </param>
        ''' <returns>The result of the (evaluated) function call.</returns>
        <ExportAPI("do.call")>
        Public Function doCall(what As Object, calls$,
                               <RListObjectArgument>
                               Optional args As Object = Nothing,
                               Optional envir As Environment = Nothing) As Object

            If what Is Nothing OrElse calls.StringEmpty Then
                Return Internal.stop("Nothing to call!", envir)
            ElseIf what.GetType Is GetType(String) Then
                ' call static api by name
                Return CallInternal(what, args, envir)
            End If

            Dim targetType As Type = what.GetType

            ' call api from an object instance 
            If targetType Is GetType(vbObject) Then
                Dim member = DirectCast(what, vbObject).getByName(name:=calls)

                ' invoke .NET API / property getter
                If member.GetType Is GetType(RMethodInfo) Then
                    Dim arguments As InvokeParameter() = args
                    Dim api As RMethodInfo = DirectCast(member, RMethodInfo)

                    Return api.Invoke(envir, arguments)
                Else
                    Return member
                End If
            Else
                Return Internal.stop(New NotImplementedException(targetType.FullName), envir)
            End If
        End Function

        Public Function CallInternal(call$, args As Object, envir As Environment) As Object
            Return Internal.stop(New NotImplementedException("Call internal functions"), envir)
        End Function
    End Module
End Namespace