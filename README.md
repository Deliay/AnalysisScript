# Analysis Script
A super mini script for log analysis

## Examples

### Basic usage (with BFL)
```csharp
using AnalysisScript.Library;

// initialize variables
var variables = new VariableContext()
    .AddInitializeVariable("a", 1)
    .AddInitialzieVariable("b", new List<int>() { 2, 3, 4 });

// register custom methods
var incrSingle = (int x) => ValueTask.FromResult(p + 1);
var incrSequence = (IEnumerable<int> xs)
    => ValueTask.CompleteTask(xs.Select(x => x + 1));

variables.Methods.RegisterInstanceFunction("inc", incrSingle);
variables.Methods.RegisterInstanceFunction("inc", incrSequence);

// run code and get return value
var interpreter = AsInterpreter.Of(variables,
"""
param a
param b

let first = c
| inc

let tail = d
| inc
| join "," 

let res = "${first},${tail}"

return res
""");
var result = await interpreter.RunAndReturn<string>();

// Output: 2,3,4,5
Console.WriteLine(result);
```

### All grammar example
```
# define variable 'a'
param a

let b = arg1
| fn arg2 arg3
| fn2 arg2
| fn3

call fn4 arg1 arg2 arg3

let d = "string interpolation ${c}"
let e = "string ${c} interpolation"

return d
```

## Grammar
```
param -> identity

arr -> param

arrs -> '[' arrs arr ',' | empty ']'

argument -> number | string | identity | arrs

arguments -> arguments argument

pipe -> pipe identity argument newline

pipes -> pipes pipe | empty

variable -> identity

let -> let variable = argument [pipes]

ui -> ui newline pipes

cmd -> let | ui | comment | param

return -> return identity

cmds -> cmds cmd | empty

analysis -> cmds return
```