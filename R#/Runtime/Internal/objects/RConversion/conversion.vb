﻿#Region "Microsoft.VisualBasic::d69829bfe855577be9874abaca0d5f98, R#\Runtime\Internal\objects\RConversion\conversion.vb"

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

    '     Module RConversion
    ' 
    '         Function: asCharacters, asDataframe, asDate, asInteger, asList
    '                   asLogicals, asNumeric, asObject, asPipeline, asRaw
    '                   asVector, castArrayOfGeneric, castArrayOfObject, castType, handleUnsure
    '                   isCharacter, isLogical, tryUnlistArray, unlist, unlistOfRList
    '                   unlistRecursive
    ' 
    ' 
    ' /********************************************************************************/

#End Region

Imports System.IO
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Logging
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Emit.Delegates
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Serialization
Imports Microsoft.VisualBasic.Text
Imports SMRUCC.Rsharp.Interpreter
Imports SMRUCC.Rsharp.Interpreter.ExecuteEngine.ExpressionSymbols
Imports SMRUCC.Rsharp.Runtime.Components
Imports SMRUCC.Rsharp.Runtime.Internal.Invokes
Imports SMRUCC.Rsharp.Runtime.Internal.Invokes.LinqPipeline
Imports SMRUCC.Rsharp.Runtime.Interop
Imports REnv = SMRUCC.Rsharp.Runtime
Imports Rset = SMRUCC.Rsharp.Runtime.Internal.Invokes.set
Imports any = Microsoft.VisualBasic.Scripting

Namespace Runtime.Internal.Object.Converts

    Module RConversion

        ''' <summary>
        ''' parse string text content as date time values
        ''' </summary>
        ''' <param name="obj"></param>
        ''' <param name="env"></param>
        ''' <returns></returns>
        <ExportAPI("as.date")>
        Public Function asDate(<RRawVectorArgument> obj As Object, Optional env As Environment = Nothing) As Date()
            Return Rset _
                .getObjectSet(obj, env) _
                .Select(Function(o)
                            If TypeOf o Is Date Then
                                Return CDate(o)
                            Else
                                Return Date.Parse(any.ToString(o))
                            End If
                        End Function) _
                .ToArray
        End Function

        ''' <summary>
        ''' Cast .NET object to R# object
        ''' </summary>
        ''' <param name="obj"></param>
        ''' <returns></returns>
        <ExportAPI("as.object")>
        Public Function asObject(<RRawVectorArgument> obj As Object) As Object
            If obj Is Nothing Then
                Return Nothing
            Else
                Dim type As Type = obj.GetType

                Select Case type
                    Case GetType(vbObject), GetType(vector), GetType(list)
                        Return obj
                    Case GetType(RReturn)
                        Return asObject(DirectCast(obj, RReturn).Value)
                    Case Else
                        If type.IsArray Then
                            Return Runtime.asVector(Of Object)(obj) _
                                .AsObjectEnumerator _
                                .Select(Function(o) New vbObject(o)) _
                                .ToArray
                        Else
                            Return New vbObject(obj, type)
                        End If
                End Select
            End If
        End Function

        ''' <summary>
        ''' ### Flatten Lists
        ''' 
        ''' Given a list structure x, unlist simplifies it to produce a vector 
        ''' which contains all the atomic components which occur in <paramref name="x"/>.
        ''' </summary>
        ''' <param name="x">an R Object, typically a list Or vector.</param>
        ''' <param name="[typeof]">element type of the array</param>
        ''' <param name="env"></param>
        ''' <returns>
        ''' NULL or an expression or a vector of an appropriate mode to hold 
        ''' the list components.
        '''
        ''' The output type Is determined from the highest type Of the components 
        ''' In the hierarchy ``NULL`` &lt; ``raw`` &lt; ``logical`` &lt; ``Integer`` 
        ''' &lt; ``Double`` &lt; ``complex`` &lt; ``character`` &lt; ``list`` &lt; 
        ''' ``expression``, after coercion Of pairlists To lists.
        ''' </returns>
        ''' <remarks>
        ''' unlist is generic: you can write methods to handle specific classes of 
        ''' objects, see InternalMethods, and note, e.g., relist with the unlist
        ''' method for relistable objects.
        '''
        ''' If recursive = False, the Function will Not recurse beyond the first level 
        ''' items In x.
        '''
        ''' Factors are treated specially. If all non-list elements Of x are factors 
        ''' (Or ordered factors) Then the result will be a factor With levels the union 
        ''' Of the level sets Of the elements, In the order the levels occur In the 
        ''' level sets Of the elements (which means that If all the elements have the 
        ''' same level Set, that Is the level Set Of the result).
        '''
        ''' x can be an atomic vector, but Then unlist does Nothing useful, Not even 
        ''' drop names.
        '''
        ''' By Default, unlist tries to retain the naming information present in x. If 
        ''' ``use.names = FALSE`` all naming information Is dropped.
        '''
        ''' Where possible the list elements are coerced To a common mode during the 
        ''' unlisting, And so the result often ends up As a character vector. Vectors 
        ''' will be coerced To the highest type Of the components In the hierarchy 
        ''' ``NULL`` &lt; ``raw`` &lt; ``logical`` &lt; ``Integer`` &lt; ``Double`` &lt;
        ''' ``complex`` &lt; ``character`` &lt; ``list`` &lt; ``expression``: pairlists 
        ''' are treated As lists.
        '''
        ''' A list Is a (generic) vector, And the simplified vector might still be a list 
        ''' (And might be unchanged). Non-vector elements Of the list (For example language 
        ''' elements such As names, formulas And calls) are Not coerced, And so a list 
        ''' containing one Or more Of these remains a list. (The effect Of unlisting an lm 
        ''' fit Is a list which has individual residuals As components.) Note that 
        ''' ``unlist(x)`` now returns x unchanged also For non-vector x, instead Of signalling 
        ''' an Error In that Case.
        ''' </remarks>
        <ExportAPI("unlist")>
        Public Function unlist(<RRawVectorArgument> x As Object,
                               Optional [typeof] As Object = Nothing,
                               Optional pipeline As Boolean = False,
                               Optional env As Environment = Nothing) As Object

            If x Is Nothing Then
                Return Nothing
            End If

            If Not [typeof] Is Nothing Then
                [typeof] = env.globalEnvironment.GetType([typeof])
            End If
            If TypeOf [typeof] Is Type Then
                [typeof] = RType.GetRSharpType(DirectCast([typeof], Type))
            End If

            Dim containsListNames As Value(Of Boolean) = False
            Dim result As IEnumerable = unlistRecursive(x, containsListNames)

            If pipeline Then
                Return New pipeline(result, TryCast([typeof], RType))
            Else
                If containsListNames.Value Then
                    Dim names As New List(Of String)
                    Dim values As New List(Of Object)

                    For Each obj In result
                        If obj Is Nothing Then
                            names.Add("")
                            values.Add(Nothing)
                        ElseIf TypeOf obj Is NamedValue(Of Object) Then
                            With DirectCast(obj, NamedValue(Of Object))
                                names.Add(.Name)
                                values.Add(.Value)
                            End With
                        Else
                            names.Add("")
                            values.Add(obj)
                        End If
                    Next

                    If [typeof] Is Nothing Then
                        Return New vector(names.ToArray, values.ToArray(Of Object), env)
                    Else
                        Return New vector(names.ToArray, values.ToArray(Of Object), [typeof], env)
                    End If
                Else
                    Dim objs As Array = result.ToArray(Of Object)

                    If [typeof] Is Nothing Then
                        Return objs.TryCastGenericArray(env)
                    Else
                        Return New vector(objs, [typeof])
                    End If
                End If
            End If
        End Function

        Private Function unlistRecursive(x As Object, containsListNames As Value(Of Boolean)) As IEnumerable
            If x Is Nothing Then
                Return New Object() {}
            Else
                Dim listType As Type = x.GetType

                If listType.IsArray Then
                    Return tryUnlistArray(DirectCast(x, Array), containsListNames)
                ElseIf listType Is GetType(vector) Then
                    Return tryUnlistArray(DirectCast(x, vector).data, containsListNames)
                ElseIf DataFramework.IsPrimitive(listType) Then
                    Return {x}
                ElseIf listType Is GetType(list) Then
                    Return DirectCast(x, list).unlistOfRList(containsListNames)
                ElseIf listType.ImplementInterface(GetType(IDictionary)) Then
                    Return New list(DirectCast(x, IDictionary)).unlistOfRList(containsListNames)
                ElseIf listType Is GetType(pipeline) Then
                    Return tryUnlistArray(DirectCast(x, pipeline).pipeline, containsListNames)
                Else
                    ' Return Internal.debug.stop(New InvalidCastException(list.GetType.FullName), env)
                    ' is a single uer defined .NET object 
                    Return {x}
                End If
            End If
        End Function

        <Extension>
        Private Iterator Function tryUnlistArray(data As IEnumerable, containsListNames As Value(Of Boolean)) As IEnumerable
            For Each obj As Object In data
                For Each item As Object In unlistRecursive(obj, containsListNames)
                    Yield item
                Next
            Next
        End Function

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="rlist"></param>
        ''' <returns></returns>
        <Extension>
        Private Function unlistOfRList(rlist As list, containsListNames As Value(Of Boolean)) As IEnumerable
            Dim data As New List(Of NamedValue(Of Object))

            For Each name As String In rlist.getNames
                Dim a As Array = Runtime.asVector(Of Object)(rlist.slots(name)).tryUnlistArray(containsListNames).ToArray(Of Object)

                If a.Length = 1 Then
                    data.Add(New NamedValue(Of Object)(name, a.GetValue(Scan0)))
                Else
                    Dim i As i32 = 1

                    For Each item As Object In a
                        data.Add(New NamedValue(Of Object)($"{name}{++i}", item))
                    Next
                End If
            Next

            containsListNames.Value = True

            Return data
        End Function

        ''' <summary>
        ''' ### Coerce to a Data Frame
        ''' 
        ''' Functions to check if an object is a data frame, or coerce it if possible.
        ''' </summary>
        ''' <param name="x">any R object.</param>
        ''' <param name="env"></param>
        ''' <returns></returns>
        <ExportAPI("as.data.frame")>
        <RApiReturn(GetType(dataframe))>
        Public Function asDataframe(<RRawVectorArgument> x As Object,
                                    <RListObjectArgument> args As Object,
                                    Optional env As Environment = Nothing) As Object
            If x Is Nothing Then
                Return x
            ElseIf TypeOf X Is dataframe Then
                ' clone
                Dim raw As dataframe = x
                Dim clone As New dataframe With {
                    .columns = raw.columns _
                        .ToDictionary(Function(k) k.Key,
                                      Function(k)
                                          Return k.Value
                                      End Function),
                    .rownames = If(raw.rownames.IsNullOrEmpty, Nothing, raw.rownames.ToArray)
                }

                Return clone
            ElseIf x.GetType.IsArray AndAlso DirectCast(x, Array).Length = 0 Then
                Return Nothing
            Else
                args = base.Rlist(args, env)

                If Program.isException(args) Then
                    Return args
                End If
            End If
RE0:
            Dim type As Type = x.GetType

            If type Is GetType(list) Then
                Return makeDataframe.fromList(DirectCast(x, list), env)
            ElseIf makeDataframe.is_ableConverts(type) Then
                Return makeDataframe.createDataframe(type, x, args, env)
            Else
                type = makeDataframe.tryTypeLineage(type)

                If type Is Nothing Then
                    Dim err As Message = handleUnsure(x, type, env)

                    If Not err Is Nothing Then
                        Return err
                    Else
                        GoTo RE0
                    End If
                Else
                    Return makeDataframe.createDataframe(type, x, args, env)
                End If
            End If
        End Function

        Private Function handleUnsure(ByRef x As Object, ByRef type As Type, env As Environment) As Message
            If TypeOf x Is vector Then
                x = DirectCast(x, vector).data
                type = MeasureRealElementType(x)

                If x.GetType.GetElementType Is Nothing OrElse x.GetType.GetElementType Is GetType(Object) Then
                    Dim list = Array.CreateInstance(type, DirectCast(x, Array).Length)

                    With DirectCast(x, Array)
                        For i As Integer = 0 To .Length - 1
                            x = .GetValue(i)

                            If TypeOf x Is vbObject Then
                                x = DirectCast(x, vbObject).target
                            End If

                            list.SetValue(x, i)
                        Next
                    End With

                    x = list
                End If

                Return Nothing
            End If

            If type Is Nothing Then
                Return Internal.debug.stop(New InvalidProgramException("unknown type handler..."), env)
            Else
                Return Internal.debug.stop(New InvalidProgramException("missing api for handle of data: " & type.FullName), env)
            End If
        End Function

        ''' <summary>
        ''' ### Vectors
        ''' 
        ''' vector produces a vector of the given length and mode.
        ''' 
        ''' as.vector, a generic, attempts to coerce its argument into a 
        ''' vector of mode mode (the default is to coerce to whichever 
        ''' vector mode is most convenient): if the result is atomic all 
        ''' attributes are removed.
        ''' </summary>
        ''' <param name="x">an R object.</param>
        ''' <param name="mode">
        ''' character string naming an atomic mode or "list" or "expression" 
        ''' or (except for vector) "any". Currently, is.vector() allows any 
        ''' type (see typeof) for mode, and when mode is not "any", 
        ''' ``is.vector(x, mode)`` is almost the same as ``typeof(x) == mode``.
        ''' </param>
        ''' <param name="env"></param>
        ''' <remarks>
        ''' The atomic modes are "logical", "integer", "numeric" (synonym 
        ''' "double"), "complex", "character" and "raw".
        '''
        ''' If mode = "any", Is.vector may Return True For the atomic modes, 
        ''' list And expression. For any mode, it will Return False If x has 
        ''' any attributes except names. (This Is incompatible With S.) On 
        ''' the other hand, As.vector removes all attributes including names 
        ''' For results Of atomic mode (but Not those Of mode "list" nor 
        ''' "expression").
        '''
        ''' Note that factors are Not vectors; Is.vector returns False And 
        ''' ``as.vector`` converts a factor To a character vector For 
        ''' ``mode = "any"``.
        ''' 
        ''' as.vector and is.vector are quite distinct from the meaning of the 
        ''' formal class "vector" in the methods package, and hence as(x, "vector") 
        ''' and is(x, "vector").
        '''
        ''' Note that ``as.vector(x)`` Is Not necessarily a null operation if 
        ''' ``is.vector(x)`` Is true: any names will be removed from an atomic 
        ''' vector.
        '''
        ''' Non-vector modes "symbol" (synonym "name") And "pairlist" are accepted 
        ''' but have long been undocumented: they are used To implement As.name And 
        ''' As.pairlist, And those functions should preferably be used directly. 
        ''' None Of the description here applies To those modes: see the help For 
        ''' the preferred forms.
        ''' </remarks>
        ''' <returns>
        ''' For vector, a vector of the given length and mode. Logical vector 
        ''' elements are initialized to FALSE, numeric vector elements to 0, 
        ''' character vector elements to "", raw vector elements to nul bytes and 
        ''' list/expression elements to NULL.
        '''
        ''' For as.vector, a vector (atomic Or of type list Or expression). All 
        ''' attributes are removed from the result if it Is of an atomic mode, but 
        ''' Not in general for a list result. The default method handles 24 input 
        ''' types And 12 values of type: the details Of most coercions are 
        ''' undocumented And subject To change.
        '''
        ''' For Is.vector, TRUE Or FALSE. Is.vector(x, mode = "numeric") can be 
        ''' true for vectors of types "integer" Or "double" whereas 
        ''' ``is.vector(x, mode = "double")`` can only be true for those of type 
        ''' "double".
        ''' </returns>
        <ExportAPI("as.vector")>
        Public Function asVector(<RRawVectorArgument> x As Object,
                                 Optional mode As Object = "any",
                                 Optional env As Environment = Nothing) As Object

            If x Is Nothing Then
                Return x
            End If

            Dim classType As RType = env.globalEnvironment.GetType(mode)

            If TypeOf x Is vector Then
                If classType.raw Is GetType(Object) Then
                    DirectCast(x, vector).elementType = classType
                Else
                    x = New vector With {
                        .data = REnv.asVector(DirectCast(x, vector).data, classType.raw, env),
                        .elementType = classType
                    }
                End If

                Return x
            ElseIf x.GetType.IsArray Then
                Return New vector With {.data = REnv.asVector(x, classType.raw, env), .elementType = classType}
            ElseIf TypeOf x Is pipeline Then
                Return DirectCast(x, pipeline).createVector(env)
            ElseIf TypeOf x Is Group Then
                Return New vector With {.data = REnv.asVector(DirectCast(x, Group).group, classType.raw, env), .elementType = classType}
            Else
                Dim interfaces = x.GetType.GetInterfaces

                ' array of <T>
                For Each type In interfaces
                    If type.GenericTypeArguments.Length > 0 AndAlso type.ImplementInterface(Of IEnumerable) Then
                        Return type.castArrayOfGeneric(x)
                    End If
                Next

                ' array of object
                For Each type In interfaces
                    If type Is GetType(IEnumerable) Then
                        Return castArrayOfObject(x)
                    End If
                Next

                ' obj is not a vector type
                env.AddMessage($"target object of '{x.GetType.FullName}' can not be convert to a vector.", MSG_TYPES.WRN)

                Return x
            End If
        End Function

        Private Function castArrayOfObject(obj As Object) As vector
            Dim buffer As New List(Of Object)

            For Each x As Object In DirectCast(obj, IEnumerable)
                buffer.Add(x)
            Next

            Return New vector With {.data = buffer.ToArray}
        End Function

        <Extension>
        Private Function castArrayOfGeneric(type As Type, obj As Object) As vector
            Dim generic As Type = type.GenericTypeArguments(Scan0)
            Dim buffer As Object = Activator.CreateInstance(GetType(List(Of )).MakeGenericType(generic))
            Dim add As MethodInfo = buffer.GetType.GetMethod("Add", BindingFlags.Public Or BindingFlags.Instance)
            Dim source As IEnumerator = DirectCast(obj, IEnumerable).GetEnumerator

            source.MoveNext()
            source = source.Current

            ' get element count by list
            Do While source.MoveNext
                Call add.Invoke(buffer, {source.Current})
            Loop

            ' write buffered data to vector
            Dim vec As Array = Array.CreateInstance(generic, DirectCast(buffer, IList).Count)
            Dim i As i32 = Scan0

            For Each x As Object In DirectCast(buffer, IEnumerable)
                vec.SetValue(x, ++i)
            Next

            Return New vector With {.data = vec}
        End Function

        ''' <summary>
        ''' Cast the raw dictionary object to R# list object
        ''' </summary>
        ''' <param name="obj"></param>
        ''' <param name="args">
        ''' for dataframe type:
        ''' 
        ''' + ``byrow``: logical, default is FALSE, means cast dataframe to list directly by column hash table values
        ''' + ``names``: character, the column names that will be used as the list names
        ''' </param>
        ''' <returns></returns>
        <ExportAPI("as.list")>
        <RApiReturn(GetType(list))>
        Public Function asList(obj As Object, <RListObjectArgument> args As Object, Optional env As Environment = Nothing) As Object
            If obj Is Nothing Then
                Return Nothing
            Else
                Return listInternal(obj, base.Rlist(args, env), env)
            End If
        End Function

        ''' <summary>
        ''' Cast the given vector or list to integer type
        ''' </summary>
        ''' <param name="obj"></param>
        ''' <returns></returns>
        <ExportAPI("as.integer")>
        <RApiReturn(GetType(Long))>
        Public Function asInteger(<RRawVectorArgument> obj As Object, Optional env As Environment = Nothing) As Object
            If obj Is Nothing Then
                Return 0
            ElseIf obj.GetType.ImplementInterface(GetType(IDictionary)) Then
                Return Runtime.CTypeOfList(Of Long)(obj, env)
            ElseIf obj.GetType.IsArray Then
                Dim type As Type = MeasureRealElementType(obj)

                If type Is GetType(String) Then
                    ' 20200427 try to fix bugs on linux platform 
                    ' 
                    ' Error in <globalEnvironment> -> InitializeEnvironment -> str_pad -> str_pad -> as.integer -> as.integer
                    ' 1. TargetInvocationException: Exception has been thrown by the target of an invocation.
                    ' 2. DllNotFoundException: kernel32
                    ' 3. stackFrames: 
                    ' at System.Reflection.MonoMethod.Invoke (System.Object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, System.Object[] parameters, System.Globalization.CultureInfo culture) [0x00083] in <47d423fd1d4342b9832b2fe1f5d431eb>:0 
                    ' at System.Reflection.MethodBase.Invoke (System.Object obj, System.Object[] parameters) [0x00000] in <47d423fd1d4342b9832b2fe1f5d431eb>:0 
                    ' at SMRUCC.Rsharp.Runtime.Interop.RMethodInfo.Invoke (System.Object[] parameters, SMRUCC.Rsharp.Runtime.Environment env) [0x00073] in <f2d41b7896b5443b8d9c40b31555c1b7>:0 

                    ' R# source: mapId <- Call str_pad(Call as.integer(Call /\d+/(&mapId)), 5, left, 0)

                    ' RConversion.R#_interop::.as.integer at REnv.dll:line <unknown>
                    ' SMRUCC/R#.call_function.as.integer at renderMap_CLI.R:line 17
                    ' stringr.R#_interop::.str_pad at REnv.dll:line <unknown>
                    ' SMRUCC/R#.call_function.str_pad at renderMap_CLI.R:line 17
                    ' SMRUCC/R#.n/a.InitializeEnvironment at renderMap_CLI.R:line 0
                    ' SMRUCC/R#.global.<globalEnvironment> at <globalEnvironment>:line n/a
                    Return DirectCast(Runtime.asVector(Of String)(obj), String()) _
                        .Select(AddressOf Long.Parse) _
                        .ToArray
                Else
                    Return Runtime.asVector(Of Long)(obj)
                End If
            Else
                Return Runtime.asVector(Of Long)(obj)
            End If
        End Function

        ''' <summary>
        ''' # Numeric Vectors
        ''' 
        ''' Creates or coerces objects of type "numeric". is.numeric is a more 
        ''' general test of an object being interpretable as numbers.
        ''' </summary>
        ''' <param name="x">object to be coerced or tested.</param>
        ''' <param name="env"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' ``NULL`` will be treated as ZERO based on the rule of VisualBasic runtime conversion.
        ''' </remarks>
        <ExportAPI("as.numeric")>
        <RApiReturn(GetType(Double))>
        Public Function asNumeric(<RRawVectorArgument> x As Object, Optional env As Environment = Nothing) As Object
            If x Is Nothing Then
                Return 0
            End If

            If TypeOf x Is list Then
                x = DirectCast(x, list).slots
            End If

            If x.GetType.ImplementInterface(GetType(IDictionary)) Then
                Return Runtime.CTypeOfList(Of Double)(x, env)
            ElseIf TypeOf x Is Double() Then
                Return New vector(x, RType.GetRSharpType(GetType(Double)))
            ElseIf TypeOf x Is vector AndAlso DirectCast(x, vector).elementType Like RType.floats Then
                Return New vector(x)
            Else
                Dim data As Object() = pipeline _
                    .TryCreatePipeline(Of Object)(x, env) _
                    .populates(Of Object)(env) _
                    .ToArray
                Dim populates = Iterator Function() As IEnumerable(Of Double)
                                    For Each item In data
                                        If item Is Nothing Then
                                            Yield 0
                                        ElseIf TypeOf item Is String Then
                                            Yield Val(DirectCast(item, String))
                                        ElseIf TypeOf item Is Double Then
                                            Yield DirectCast(item, Double)
                                        Else
                                            Yield DirectCast(RCType.CTypeDynamic(item, GetType(Double), env), Double)
                                        End If
                                    Next
                                End Function

                Return populates().ToArray
            End If
        End Function

        ''' <summary>
        ''' ### Character Vectors
        ''' 
        ''' Create or test for objects of type "character".
        ''' </summary>
        ''' <param name="x">object to be coerced or tested.</param>
        ''' <param name="env"></param>
        ''' <returns>
        ''' as.character attempts to coerce its argument to character type; 
        ''' like as.vector it strips attributes including names. For lists 
        ''' and pairlists (including language objects such as calls) it deparses 
        ''' the elements individually, except that it extracts the first element 
        ''' of length-one character vectors.
        ''' </returns>
        ''' <remarks>
        ''' as.character and is.character are generic: you can write methods 
        ''' to handle specific classes of objects, see InternalMethods. 
        ''' Further, for as.character the default method calls as.vector, 
        ''' so dispatch is first on methods for as.character and then for methods 
        ''' for as.vector.
        '''
        ''' as.character represents real And complex numbers to 15 significant 
        ''' digits (technically the compiler's setting of the ISO C constant 
        ''' DBL_DIG, which will be 15 on machines supporting IEC60559 arithmetic 
        ''' according to the C99 standard). This ensures that all the digits in 
        ''' the result will be reliable (and not the result of representation 
        ''' error), but does mean that conversion to character and back to numeric 
        ''' may change the number. If you want to convert numbers to character 
        ''' with the maximum possible precision, use format.
        ''' </remarks>
        <ExportAPI("as.character")>
        <RApiReturn(GetType(String))>
        Public Function asCharacters(<RRawVectorArgument> x As Object, Optional env As Environment = Nothing) As Object
            If x Is Nothing Then
                Return Nothing
            ElseIf x.GetType.ImplementInterface(GetType(IDictionary)) Then
                Return Runtime.CTypeOfList(Of String)(x, env)
            Else
                Return Runtime.asVector(Of String)(x)
            End If
        End Function

        ''' <summary>
        ''' ### Character Vectors
        ''' 
        ''' Create or test for objects of type "character".
        ''' </summary>
        ''' <param name="x">object to be coerced or tested.</param>
        ''' <returns>is.character returns TRUE or FALSE depending on 
        ''' whether its argument is of character type or not.
        ''' </returns>
        <ExportAPI("is.character")>
        Public Function isCharacter(<RRawVectorArgument> x As Object) As Boolean
            If x Is Nothing Then
                Return False
            ElseIf x.GetType Is GetType(vector) Then
                x = DirectCast(x, vector).data
            ElseIf x.GetType Is GetType(list) Then
                ' 只判断list的value
                x = DirectCast(x, list).slots.Values.ToArray
            ElseIf x.GetType.ImplementInterface(GetType(IDictionary)) Then
                x = DirectCast(x, IDictionary).Values.AsSet.ToArray
            End If

            If x.GetType Like RType.characters Then
                Return True
            ElseIf x.GetType.IsArray AndAlso DirectCast(x, Array) _
                .AsObjectEnumerator _
                .All(Function(xi)
                         Return xi.GetType Like RType.characters
                     End Function) Then

                Return True
            Else
                Return False
            End If
        End Function

        <ExportAPI("is.logical")>
        Public Function isLogical(<RRawVectorArgument> x As Object) As Boolean
            If x Is Nothing Then
                Return False
            ElseIf x.GetType Is GetType(vector) Then
                x = DirectCast(x, vector).data
            ElseIf x.GetType Is GetType(list) Then
                ' 只判断list的value
                x = DirectCast(x, list).slots.Values.ToArray
            ElseIf x.GetType.ImplementInterface(GetType(IDictionary)) Then
                x = DirectCast(x, IDictionary).Values.AsSet.ToArray
            End If

            If x.GetType Like RType.logicals Then
                Return True
            ElseIf x.GetType.IsArray AndAlso DirectCast(x, Array) _
                .AsObjectEnumerator _
                .All(Function(xi)
                         Return xi.GetType Like RType.logicals
                     End Function) Then

                Return True
            Else
                Return False
            End If
        End Function

        <ExportAPI("as.logical")>
        <RApiReturn(GetType(Boolean))>
        Public Function asLogicals(<RRawVectorArgument> obj As Object, Optional env As Environment = Nothing) As Object
            If obj Is Nothing Then
                Return Nothing
            ElseIf obj.GetType.ImplementInterface(GetType(IDictionary)) Then
                Return Runtime.CTypeOfList(Of Boolean)(obj, env)
            Else
                Return Runtime.asLogical(obj)
            End If
        End Function

        ''' <summary>
        ''' cast any R# object to raw buffer
        ''' </summary>
        ''' <param name="obj">numeric values or character vector</param>
        ''' <param name="env"></param>
        ''' <returns></returns>
        <ExportAPI("as.raw")>
        Public Function asRaw(<RRawVectorArgument> obj As Object,
                              Optional encoding As Encodings = Encodings.UTF8WithoutBOM,
                              Optional networkByteOrder As Boolean = True,
                              Optional env As Environment = Nothing) As Object

            If obj Is Nothing Then
                Return Nothing
            ElseIf TypeOf obj Is RawStream Then
                Return New MemoryStream(DirectCast(obj, RawStream).Serialize)
            Else
                Return env.castToRawRoutine(obj, encoding, networkByteOrder)
            End If
        End Function

        ''' <summary>
        ''' running pipeline function in linq pipeline mode
        ''' </summary>
        ''' <param name="seq">any kind of object sequence in R# environment</param>
        ''' <returns></returns>
        <ExportAPI("as.pipeline")>
        Public Function asPipeline(<RRawVectorArgument> seq As Object, Optional env As Environment = Nothing) As pipeline
            Dim type As RType = Nothing
            Dim sequence = Rset.getObjectSet(seq, env, elementType:=type)

            Return New pipeline(sequence, type)
        End Function

        ''' <summary>
        ''' try to cast type class of the given data sequence 
        ''' </summary>
        ''' <param name="any"></param>
        ''' <param name="type"></param>
        ''' <param name="env"></param>
        ''' <returns></returns>
        <ExportAPI("ctype")>
        Public Function castType(<RRawVectorArgument> any As Object, type As String, Optional env As Environment = Nothing) As Object
            Dim [class] As [Variant](Of RType, Message) = DataSets.CreateObject.TryGetType(type, env)

            If [class] Like GetType(Message) Then
                Return [class].TryCast(Of Message)
            End If

            If any Is Nothing Then
                Return Nothing
            ElseIf TypeOf any Is pipeline Then
                Dim pip As pipeline = DirectCast(any, pipeline)
                pip.elementType = [class].TryCast(Of RType)
                Return pip
            ElseIf TypeOf any Is vector Then
                Dim vec As vector = DirectCast(any, vector)
                vec.elementType = [class].TryCast(Of RType)
                Return vec
            ElseIf TypeOf any Is Array Then
                Dim arr As Array = DirectCast(any, Array)
                Dim vec As New vector(arr, [class].TryCast(Of RType))
                Return vec
            Else
                Return Internal.debug.stop(Message.InCompatibleType(GetType(pipeline), any.GetType, env), env)
            End If
        End Function
    End Module
End Namespace
