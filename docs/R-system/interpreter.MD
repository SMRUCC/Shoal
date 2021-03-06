<!-- vscode-markdown-toc -->
* 1. [Statements in R#](#StatementsinR)
		* 1.1. [Variable Declares](#VariableDeclares)
		* 1.2. [``if`` branch statement](#ifbranchstatement)
		* 1.3. [``function`` declares statement](#functiondeclaresstatement)
		* 1.4. [``for`` loop statement](#forloopstatement)
		* 1.5. [``do`` loop statement](#doloopstatement)

<!-- vscode-markdown-toc-config
	numbering=true
	autoSave=true
	/vscode-markdown-toc-config -->
<!-- /vscode-markdown-toc -->

# How the interpreter works?

```
script -> [TokenIcer] -> SourceTree -> ListOf RStatements -> ExecuteMachine
```

##  1. <a name='StatementsinR'></a>Statements in R#

####  1.1. <a name='VariableDeclares'></a>Variable Declares

```R
# Generic type variable
var {variable_name|tuple} <- {initial_expression};
```

By default the R variable is generic type, but the user can constraint the variable to a specific primitive type. This type constraint feature only works on the R primitive type(which means the type constraint will not works on the list type or tuple type in R#), which their type codes can be found in this declare:

```vbnet
''' <summary>
''' The R# types
''' </summary>
Public Enum TypeCodes As Byte

    ''' <summary>
    ''' Object type in R#
    ''' </summary>
    [generic] = 0

    ''' <summary>
    ''' Class type in R#
    ''' </summary>
    [list] = 10

    ''' <summary>
    ''' <see cref="Integer"/> vector
    ''' </summary>
    [integer] = 100
    ''' <summary>
    ''' <see cref="ULong"/> vector
    ''' </summary>
    [uinteger]
    ''' <summary>
    ''' <see cref="Double"/> numeric vector
    ''' </summary>
    [double]
    ''' <summary>
    ''' <see cref="String"/> vector
    ''' </summary>
    [string]
    ''' <summary>
    ''' <see cref="Char"/> vector
    ''' </summary>
    [char]
    ''' <summary>
    ''' <see cref="Boolean"/> vector
    ''' </summary>
    [boolean]

End Enum
```

Example:

```R
# R variable with primitive type constraint
# type = {integer, uinteger, double, string, char, boolean}
let {variable} as {type} <- {initial_expression};
```

####  1.2. <a name='ifbranchstatement'></a>``if`` branch statement

```R
if ({bool_expression}) {
    # branch statements for TRUE
} else {
    # branch statements for FALSE
    # branch FALSE can be optional
}
```

A more complex ``if`` statement:

```R
if ({bool_expression}) {
    # branch statements for TRUE = {bool_expression}
} else if ({bool_expression2}) {
    # branch statements for TRUE = {bool_expression2}
} else {
    # branch statements for FALSE
    # branch FALSE can be optional
}
```

####  1.3. <a name='functiondeclaresstatement'></a>``function`` declares statement

```R
{function_name} <- function(parameter1 [as {type}], [...]) {
    # function body closure
}
```

The function parameters, can be:

| name                   | description                      | syntax                                            |
|------------------------|----------------------------------|---------------------------------------------------|
| ``generic type``       | only an object identifier name   | ``{identifier}``                                  |
| ``primitive type``     | identifier with type constraint  | ``{identifier} as {type}``                        |
| ``parameter list``     | generic list parameters          | ``...``                                           |
| ``optional parameter`` | parameter have its default value | ``{identifier} [as {type}] = {value_expression}`` |

Supports of the optional parameter scripting:

```R
{function_name} <- function(var1, var2, optional = {expression<var1, var2>}) {
    # The expression can be any type of the value expression statement
    # no limits to the constant value
    # allowed the user types like: function, tuple and list class
}
```

This optional parameter expression example as: 

```R
# The optional parameter c is a tuple type and 
# with 2 property which are named: x and y
test <- function(a, b, c = [x=(a + b)^2, y=128]) {
    return [a ^ 3, -b * c.x, c.y];
}
```

####  1.4. <a name='forloopstatement'></a>``for`` loop statement

The R language its ``for`` loop is equals to the ``For Each`` Loop in VisualBasic:

```R
for({variable} in {collection}) {
    # for closure statements
}
```

The for each example in R#:

```R
for (i in 1:50) {
    # for each loop closure 
}

for (n in [1:50, 0.1]) {
    
    # which it is equivalent to 
    # var x as double <- [1:50, 0.01];
    # 
    # for (d as double in x) {
    #     # for each loop closure 
    # }
}
```

```vbnet
For Each i In (1, 50).Range
Next

For Each n In (1, 50).Range(step:=0.1)
Next

Dim x = (1, 50).Range(0.01)

For Each d As Double In x
Next
```

####  1.5. <a name='doloopstatement'></a>``do`` loop statement

```R
# do while expression is TRUE
do ({boolean_expression}) {
    # do loop closure
}

# do until expression becames TRUE
do {
    # do loop closure
} until({boolean_expression});
```
