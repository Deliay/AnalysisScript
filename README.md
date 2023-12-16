# Analysis Script
A super mini script for log analysis

## Example (with BasicFunctionV2)
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
// Output: result is: ab,ac
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

## Grammar
```
param -> identity

argument -> number | string | identity

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
