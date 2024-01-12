using AnalysisScript.Interpreter;
using AnalysisScript.Interpreter.Variables;
using AnalysisScript.Interpreter.Variables.Method;

namespace AnalysisScript.Test.Interpreter;

public class InterpreterTest
{
    [Fact]
    public void WillCreateInstanceWithSpecifiedVariables()
    {
        var variables = new VariableContext();
        var interpreter = new AsInterpreter(variables);
        
        Assert.Equal(variables, interpreter.Variables);
    }
    [Fact]
    public void WillCreateInstanceWithSpecifiedVariablesAndMethodContext()
    {
        var methods = new MethodContext();
        var variables = new VariableContext(methods);
        var interpreter = new AsInterpreter(variables);
        
        Assert.Equal(variables, interpreter.Variables);
        Assert.Equal(methods, interpreter.Variables.Methods);
    }
    [Fact]
    public async Task WillExecuteLetAndItsPipelines()
    {
        var paramA = new object();
        var execTable = new { fnAExecuted = false };
        var fnA = (AsExecutionContext ctx, object a) =>
        {
            execTable = new { fnAExecuted = true };
            return a;
        }; 
        
        var variables = new VariableContext();
        variables
            .AddInitializeVariable("a", paramA)
            .Methods.RegisterInstanceFunction("test", fnA);
        
        var interpreter = AsInterpreter.Of(variables, 
            """
            param a

            let b = a
            | test
            | test
            | test

            return b
            """);
        
        Assert.Equal(variables, interpreter.Variables);
        var result = await interpreter.RunAndReturn<object>(token: default);
        
        Assert.Equal(paramA, result);
        Assert.True(execTable.fnAExecuted);
    }
    [Fact]
    public async Task WillExecuteCallAndCallRightMethod()
    {
        var paramA = new object();
        var execTable = new { fnAExecuted = false, aCaptured = new object() };
        var fnA = (AsExecutionContext ctx, object a) =>
        {
            execTable = new { fnAExecuted = true, aCaptured = a };
            return ValueTask.CompletedTask;
        }; 
        
        var variables = new VariableContext();
        variables
            .AddInitializeVariable("a", paramA)
            .Methods.RegisterInstanceFunction("test", fnA);
        
        var interpreter = AsInterpreter.Of(variables, 
            """
            param a

            call test a

            return a
            """);
        
        Assert.Equal(variables, interpreter.Variables);
        var result = await interpreter.RunAndReturn<object>(token: default);
        
        Assert.Equal(paramA, result);
        Assert.Equal(paramA, execTable.aCaptured);
        Assert.True(execTable.fnAExecuted);
    }
    [Fact]
    public async Task WillCanUnwrapTaskCorrectly()
    {
        var paramA = new object();
        var execTable = new { fnAExecuted = false };
        var fnA = (AsExecutionContext ctx, object a) =>
        {
            execTable = new { fnAExecuted = true };
            return Task.FromResult(a);
        }; 
        
        var variables = new VariableContext();
        variables
            .AddInitializeVariable("a", paramA)
            .Methods.RegisterInstanceFunction("test", fnA);
        
        var interpreter = AsInterpreter.Of(variables, 
            """
            param a

            let b = a
            | test
            | test
            | test

            return b
            """);
        
        Assert.Equal(variables, interpreter.Variables);
        var result = await interpreter.RunAndReturn<object>(token: default);
        
        Assert.Equal(paramA, result);
        Assert.True(execTable.fnAExecuted);
    }
    [Fact]
    public async Task WillCanUnwrapValueTaskCorrectly()
    {
        var paramA = new object();
        var execTable = new { fnAExecuted = false };
        var fnA = (AsExecutionContext ctx, object a) =>
        {
            execTable = new { fnAExecuted = true };
            return ValueTask.FromResult(a);
        }; 
        
        var variables = new VariableContext();
        variables
            .AddInitializeVariable("a", paramA)
            .Methods.RegisterInstanceFunction("test", fnA);
        
        var interpreter = AsInterpreter.Of(variables, 
            """
            param a

            let b = a
            | test
            | test
            | test

            return b
            """);
        
        Assert.Equal(variables, interpreter.Variables);
        var result = await interpreter.RunAndReturn<object>(token: default);
        
        Assert.Equal(paramA, result);
        Assert.True(execTable.fnAExecuted);
    }
}