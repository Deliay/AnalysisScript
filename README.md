# Analysis Script
A super mini script for log analysis

## Examples

### Basic usage (with BFL)
```c#
using AnalysisScript.Library;

var lexicalTokens = LexicalAnalyzer.Analyze("...source...");
var ast = ScriptParser.Parse(lexicalTokens);
AsInterpreter interpreter = new AsInterpreter(ast)
.RegisterBasicFunctionsV2()
.AddVariable("a", new List<string>() { "ab", "ac", "bc" })
.AddVariable("b", "result is: ");

var result = await interpreter.RunAndReturn<string>();

Console.WriteLine(result);
// Output: result is: ab|ac
```

```
param a
param b

let c = a
| filter_regex "a."
| join "|"

let d = "${b}${a}"

return d
```

### All grammar example
```
# define variable 'a'
param a

let b = arg1
| ... call functions ...
| fn arg2 arg3
| fn2 arg2
| fn...

call fn3 arg1 arg2 arg3

# declare variable set by 'fn3'
param c

let d = "string interpolation ${c}"
let e = "string ${c} interpolation"
let f = "${c} string interpolation"

# List<String> {d, e, f}
let g = [d, e, f]

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