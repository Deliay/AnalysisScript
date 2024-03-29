using AnalysisScript.Interpreter;
using AnalysisScript.Interpreter.Variables;
using AnalysisScript.Interpreter.Variables.Method;
using AnalysisScript.Library;

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
        const int paramA = 1;
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
        const int paramA = 1;
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
    [Fact]
    public async Task CanBlockArgumentSpread()
    {
        const int paramA = 1;
        var incr = (AsExecutionContext ctx, IEnumerable<int> seq, int a) => seq.Select(c => c + a);
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
            || incr b &
            | sum

            return c
            """);

        Assert.Equal(variables, interpreter.Variables);
        var result = await interpreter.RunAndReturn<int>(token: default);

        Assert.Equal(9, result);
    }
    [Fact]
    public async Task ThrowIfBlockArgumentSpreadMissingArgumentForMethod()
    {
        const int paramA = 1;
        var incr = (AsExecutionContext ctx, IEnumerable<int> seq, int a) => seq.Select(c => c + a);
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
            || incr &
            | sum

            return c
            """);

        await Assert.ThrowsAsync<AsRuntimeException>(async () =>
        {
            _ = await interpreter.RunAndReturn<int>(token: default);
        });
    }

    [Fact]
    public async Task CanReturnArray()
    {
        var interpreter = AsInterpreter.Of($@"
let a = [1,2,3]
return a
");
        var ret = await interpreter.RunAndReturn<IEnumerable<int>>(token: default);
        
        Assert.Equal(6, ret.Sum());
    }
    [Fact]
    public async Task CanDereferenceAndReturnArray()
    {
        var variables = new VariableContext();
        variables.Methods.RegisterStaticFunction("id", Id<int[]>);
        
        var interpreter = AsInterpreter.Of(variables, $@"
let a = 1
let b = 2
let c = 3
|| id [a, b, &]
return c
");
        var ret = await interpreter.RunAndReturn<IEnumerable<int>>(token: default);
        
        Assert.Equal(6, ret.Sum());
        
        return;

        T Id<T> (AsExecutionContext ctx, T t) => t;
    }
    [Fact]
    public async Task CanPassArrayToNextPipe()
    {

        var callTable = new { called = false };
        var instanceSum = (AsExecutionContext ctx, IEnumerable<int> seq) =>
        {
            callTable = new { called = true };
            return seq.Sum();
        };
        
        var variables = new VariableContext();
        variables.Methods.RegisterStaticFunction("id", Id<int[]>);
        variables.Methods.RegisterStaticFunction("sum", Sum);
        variables.Methods.RegisterInstanceFunction("sum2", instanceSum);
        
        var interpreter = AsInterpreter.Of(variables, $@"
let a = 1
let b = 2
let c = 3
|| id [a, b, &]
| sum
|| sum2 [&, 1]
return c
");
        var ret = await interpreter.RunAndReturn<int>(token: default);
        
        Assert.Equal(7, ret);
        Assert.True(callTable.called);
        
        return;

        T Id<T> (AsExecutionContext ctx, T t) => t;
        int Sum(AsExecutionContext ctx, IEnumerable<int> seq) => seq.Sum();
    }
    [Fact]
    public async Task ThrowIfArrayTypeMismatch()
    {
        var ex = await Assert.ThrowsAsync<AsRuntimeException>(async () =>
        {
            var interpreter = AsInterpreter.Of($@"
let a = [1,2,3,""1""]
return a
");
            _ = await interpreter.RunAndReturn<IEnumerable<int>>(token: default);
        });

        Assert.IsType<InvalidArrayType>(ex.InnerException);
    }
    [Fact]
    public async Task ThrowIfReferenceTypeMismatch()
    {
        var ex = await Assert.ThrowsAsync<AsRuntimeException>(async () =>
        {
            var variables = new VariableContext();
            variables.Methods.RegisterStaticFunction("id", Id<int[]>);
            var interpreter = AsInterpreter.Of($@"
let a = ""1""
|| id [1,2,&]
return a
");
            _ = await interpreter.RunAndReturn<IEnumerable<int>>(token: default);
        });

        Assert.IsType<InvalidArrayType>(ex.InnerException);
        T Id<T> (AsExecutionContext ctx, T t) => t;
    }

    [Fact]
    public async Task WillApplyEachItemInForeachSyntax()
    {
        
        var interpreter = AsInterpreter.Of($@"
let a = [1,2,3]
|* inc

return a
");
        interpreter.Variables.Methods.RegisterStaticFunction("inc", Inc);
        var ret = await interpreter.RunAndReturn<IAsyncEnumerable<int>>(token: default);
        
        Assert.Equal(9, await ret.SumAsync());

        return;
        int Inc(AsExecutionContext ctx, int val) => val + 1;
    }
    [Fact]
    public async Task WillBlockPipeAndApplyEachItemInForeachSyntax()
    {
        var variables = new VariableContext();

        variables.Methods.RegisterStaticFunction("inc", Inc);
        variables.Methods.RegisterStaticFunction("inc", IncSeq);
        variables.Methods.RegisterStaticFunction("inc_async", IncAsync);
        var interpreter = AsInterpreter.Of(variables, $@"
let a = [1,2,3]
||* inc &
|* inc_async
| inc

return a
");
        var ret = await interpreter.RunAndReturn<IAsyncEnumerable<int>>(token: default);
        
        Assert.Equal(15, await ret.SumAsync());

        return;

        IAsyncEnumerable<int> IncSeq(AsExecutionContext ctx, IAsyncEnumerable<int> seq) => seq.Select(i => i + 1);

        Task<int> IncAsync(AsExecutionContext ctx, int val) => Task.FromResult(val + 1);

        int Inc(AsExecutionContext ctx, int val) => val + 1;
    }

    [Fact]
    public async Task ShouldMatchGenericMethodRecursively()
    {
        var variables = new VariableContext();
        variables.Methods.ScanAndRegisterStaticFunction(typeof(BasicFunctionV2));
        variables.AddInitializeVariable<IEnumerable<string>>("a", ["1", "2"]);

        var interpreter = AsInterpreter.Of(variables, $@"
param a

let b = a
| take 3
||* format ""1-2-${{&}}-3-4""  
||* format ""${{&}}-formatted""
||* split & ""-""
| flat
| join "",""

return b
");
        var ret = await interpreter.RunAndReturn<string>(token: default);
        
        Assert.Equal("1,2,1,3,4,formatted,1,2,2,3,4,formatted", ret);
        
        return;

        List<string> Split(string delim, string str) => str.Split(delim).ToList();
    }
}