using AnalysisScript.Interpreter;
using AnalysisScript.Interpreter.Variables;
using AnalysisScript.Interpreter.Variables.Method;
using System.Runtime.Intrinsics.X86;

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
        var execTable = new { fnAExecuted = false, count = 0 };
        var fnA = (AsExecutionContext ctx, object a) =>
        {
            execTable = new { fnAExecuted = true, count = execTable.count + 1 };
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
        Assert.Equal(3, execTable.count);
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
        var execTable = new { fnAExecuted = false, count = 0 };
        var fnA = (AsExecutionContext ctx, object a) =>
        {
            execTable = new { fnAExecuted = true, count = execTable.count + 1 };
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
        Assert.Equal(3, execTable.count);
    }
    [Fact]
    public async Task WillCanUnwrapValueTaskCorrectly()
    {
        var paramA = new object();
        var execTable = new { fnAExecuted = false, count = 0 };
        var fnA = (AsExecutionContext ctx, object a) =>
        {
            execTable = new { fnAExecuted = true, count = execTable.count + 1 };
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
        Assert.Equal(3, execTable.count);
    }
    [Fact]
    public async Task CanReferenceLastResultInPipe()
    {
        var paramA = 1;
        var incr = (AsExecutionContext ctx, int a, int b) => a + b;


        var variables = new VariableContext();
        variables
        .AddInitializeVariable("a", paramA)
            .Methods.RegisterInstanceFunction("incr", incr);

        var interpreter = AsInterpreter.Of(variables,
            """
            param a

            let b = a
            | incr &
            | incr &
            | incr &

            return b
            """);

        Assert.Equal(variables, interpreter.Variables);
        var result = await interpreter.RunAndReturn<int>(token: default);

        Assert.Equal(8, result);
    }
    [Fact]
    public async Task CanReferenceLastResultInPipeAndInterpolation()
    {
        var paramA = 1;
        var incr = (AsExecutionContext ctx, int a, int b, IEnumerable<int> seq) => seq.Select(c => c + a + b);
        var sum = (AsExecutionContext ctx, IEnumerable<int> seq) => seq.Sum();

        var variables = new VariableContext();
        variables
        .AddInitializeVariable("a", paramA)
        .AddInitializeVariable<IEnumerable<int>>("b", [1, 2, 3])
            .Methods.RegisterInstanceFunction("incr", incr)
                    .RegisterInstanceFunction("sum", sum);

        var interpreter = AsInterpreter.Of(variables,
            """
            param a
            param b

            let c = a
            | incr & b
            | sum

            return c
            """);

        Assert.Equal(variables, interpreter.Variables);
        var result = await interpreter.RunAndReturn<int>(token: default);

        Assert.Equal(12, result);
    }
}