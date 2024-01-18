# Analysis Script
A super mini script for data processing

## Examples

### Basic usage
```csharp
using AnalysisScript.Library;

// initialize variables
var variables = new VariableContext()
    .AddInitializeVariable("a", 1)
    .AddInitialzieVariable("b", new List<int>() { 2, 3, 4 });

// register custom methods
var incrSingle = (AsExecutionContext ctx, int x)
    => ValueTask.FromResult(p + 1);

var incrSequence = (AsExecutionContext ctx, IEnumerable<int> xs)
    => ValueTask.CompleteTask(xs.Select(x => x + 1));

var sumSequence = (AsExecutionContext ctx, IEnumerable<int> xs)
    => xs.Sum();

variables.Methods.RegisterInstanceFunction("inc", incrSingle);
variables.Methods.RegisterInstanceFunction("inc", incrSequence);
variables.Methods.RegisterInstanceFunction("sum", sumSequence);

// run code and get return value
var interpreter = AsInterpreter.Of(variables,
"""
param a
param b

let first = c
| inc

let firstArray = [0, first]
| join ","

let sum = b
| inc
| sum
# use '||' to allow '&' and make '&' reference to previous 'sum' result 
|| sum [1, &]

let res = "${firstArray}|${sum}"

return res
""");
var result = await interpreter.RunAndReturn<string>();
****
// Output: 0,2|13
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

# Use '||' to block the current value passed to the first arg of the next pipe function
# symbol '&' reference to return value of 'fn3'
|| fn4 & arg2 arg3
|| fn5 [&, arg2, arg3]

# symbol '*' can execute pipes that use 'for each' element from privious
# will throw exception if privious can't cast to 'IEnumerabe<T>'
||* fn6 arg1 & arg3

call fn6 arg1 arg2 arg3

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
