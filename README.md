# Analysis Script
A super mini script for log analysis

## Example
See project 'Playground'

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