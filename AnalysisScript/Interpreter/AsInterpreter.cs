using System.Linq.Expressions;
using AnalysisScript.Interpreter.Variables;
using AnalysisScript.Parser.Ast;
using AnalysisScript.Parser.Ast.Basic;
using AnalysisScript.Parser.Ast.Command;
using AnalysisScript.Parser.Ast.Operator;
using System.Reflection;
using System.Runtime.CompilerServices;
using AnalysisScript.Lexical;
using AnalysisScript.Parser;

namespace AnalysisScript.Interpreter;

public class AsInterpreter(AsAnalysis tree, VariableContext variableContext) : IDisposable
{
    public VariableContext Variables { get; } = variableContext;
    public IContainer? Return { get; private set; }
    public string CurrentCommand { get; private set; } = "";
    public AsAnalysis? Tree { get; set; } = tree;

    public event Action<string>? OnCommandUpdate;

    public event Action<string>? OnLogging;

    public AsInterpreter() : this(null!, new VariableContext())
    {
    }

    public AsInterpreter(VariableContext variableContext) : this(null!, variableContext)
    {
    }

    public AsInterpreter(AsAnalysis tree) : this(tree, new VariableContext())
    {
    }

    private void Logging(string message)
    {
        OnLogging?.Invoke(message);
    }

    private async ValueTask<(AsIdentity, MethodCallExpression)> RunNormalPipe(
        AsExecutionContext ctx, AsPipe pipe, MethodCallExpression previousWrappedValue)
    {
        var firstArg = pipe.DontSpreadArg ? null : previousWrappedValue;
        var pipeValueGetter = Variables.GetMethodCallLambda(firstArg, pipe.FunctionName.Name, pipe.Arguments, ctx).Compile();

        var nextValue = await pipeValueGetter();
        var sanitizedValue = await ExprTreeHelper.SanitizeLambdaExpression(nextValue);

        Variables.AddOrUpdateVariable(AsIdentity.Reference, sanitizedValue);
        var nextValueId = Variables.AddTempVar(sanitizedValue, pipe.LexicalToken);
        var wrappedValue = Variables.LambdaValueOf(nextValueId);

        return (nextValueId, wrappedValue);
    }

    private async IAsyncEnumerable<IContainer> InnerExecutePipe(
        AsExecutionContext ctx, AsPipe pipe, IAsyncEnumerable<IContainer> previousValues,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var value in previousValues.WithCancellation(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested) yield break;

            var valueLambda = ExprTreeHelper.GetConstantValueLambda(value);
            Variables.AddOrUpdateVariable(AsIdentity.Reference, value);
            
            var firstArg = pipe.DontSpreadArg ? null : valueLambda;
            var pipeValueGetter = Variables.GetMethodCallLambda(firstArg, pipe.FunctionName.Name, pipe.Arguments, ctx).Compile();
            
            var currentValue = await pipeValueGetter();
            var sanitizedValue = await ExprTreeHelper.SanitizeLambdaExpression(currentValue);

            yield return sanitizedValue;
        }
    }

    private IAsyncEnumerable<IContainer> InnerExecutePipe(
        AsExecutionContext ctx, AsPipe pipe, IAsyncEnumerable<IContainer> previousValues)
    {
        return InnerExecutePipe(ctx, pipe, previousValues, ctx.CancelToken);
    }
    
    private async ValueTask<(AsIdentity, MethodCallExpression)> RunForEachPipe(
        AsExecutionContext ctx, AsPipe pipe, MethodCallExpression previous)
    {
        var (underlying, values) = ExprTreeHelper.ConvertUnknownSequenceAsContainerizedSequence(previous);
        
        // preview method for build return value container
        var previousValue = pipe.DontSpreadArg ? null : underlying;
        var method = Variables.BuildMethod(previousValue, pipe.FunctionName.Name, pipe.Arguments, () => underlying);

        var iterated = InnerExecutePipe(ctx, pipe, values);

        var value = ExprTreeHelper.ConvertContainerSequenceAsContainerizedUnknownSequence(method.ReturnType, iterated);
        
        Variables.AddOrUpdateVariable(AsIdentity.Reference, value);
        var nextValueId = Variables.AddTempVar(value, pipe.LexicalToken);
        var wrappedValue = Variables.LambdaValueOf(nextValueId);

        return (nextValueId, wrappedValue);
    }

    private ValueTask<(AsIdentity, MethodCallExpression)> RunPipe(
        AsExecutionContext ctx, AsPipe pipe, MethodCallExpression previous)
    {
        return pipe.ForEach
            ? RunForEachPipe(ctx, pipe, previous)
            : RunNormalPipe(ctx, pipe, previous);
    }
    
    private async ValueTask<AsIdentity> RunPipe(AsExecutionContext ctx, List<AsPipe> pipes, AsObject initialValue)
    {
        var initialValueId = Variables.Storage(initialValue);
        if (pipes.Count == 0) return initialValueId;
        var initContainer = Variables.GetVariableContainer(initialValueId);
        Variables.AddOrUpdateVariable(AsIdentity.Reference, initContainer);

        var initValue = Variables.LambdaValueOf(initialValueId);
        var value = initValue;

        var lastValueId = initialValueId;
        foreach (var pipe in pipes)
        {
            ctx.CurrentExecuteObject = pipe;
            (lastValueId, value) = await RunPipe(ctx, pipe, value);
        }

        return lastValueId;
    }

    private async ValueTask ExecuteLet(AsExecutionContext ctx, AsLet let)
    {
        var runtimeValue = await RunPipe(ctx, let.Pipes, let.Arg);
        this.Variables.PutVariableContainer(let.Name, Variables.GetVariableContainer(runtimeValue));
    }

    private async ValueTask ExecuteCall(AsExecutionContext ctx, AsCall call)
    {
        var method = Variables.GetMethodCallLambda(call.Method.Name, call.Args, ctx).Compile();

        var returnValue = await method();

        await returnValue.AwaitIfUnderlyingIsKnownAwaitable();
    }

    private ValueTask ExecuteReturn(AsReturn @return)
    {
        Return = Variables.GetVariableContainer(@return.Variable);
        return ValueTask.CompletedTask;
    }

    private ValueTask ExecuteParam(AsParam param)
    {
        if (!Variables.HasVariable(param.Variable)) throw new UnknownVariableException(param.Variable);
        
        Variables.UpdateVariable(param.Variable);
        return ValueTask.CompletedTask;
    }

    [Obsolete("Use methods in 'MethodContext' for instead (this.Variables.Methods)")]
    public AsInterpreter RegisterStaticFunction(string name, MethodInfo method)
    {
        Variables.Methods.RegisterStaticFunction(name, method);
        return this;
    }
    
    [Obsolete("Use methods in 'MethodContext' for instead (this.Variables.Methods)")]
    public AsInterpreter RegisterInstanceFunction(string name, Delegate @delegate)
    {
        Variables.Methods.RegisterInstanceFunction(name, @delegate);
        return this;
    }

    public AsInterpreter AddVariable<T>(string name, T value)
    {
        Variables.PutInitVariable(new AsIdentity(new Token.Identity(name, 0, 0)), value);
        return this;
    }
    
    public ValueTask<T?> RunAndReturn<T>(CancellationToken token)
    {
        return RunAndReturn<T>(new AsExecutionContext(Logging, token));
    }

    public async ValueTask<T?> RunAndReturn<T>(AsExecutionContext ctx)
    {
        await Run(ctx);
        return Return is not null ? await Return.ValueCastToAsync<T>() : default;
    }

    public async ValueTask<T?> RunAndReturnAsync<T>(AsExecutionContext ctx)
    {
        await Run(ctx);
        return Return is not null ? await Return.ValueCastToAsync<T>() : default;
    }

    private async ValueTask RunCommand(AsExecutionContext ctx, AsCommand cmd)
    {
        CurrentCommand = cmd.ToString()!;
        OnCommandUpdate?.Invoke(CurrentCommand);

        ctx.CurrentExecuteObject = cmd;

        switch (cmd.Type)
        {
            case CommandType.Let when cmd is AsLet let:
                await ExecuteLet(ctx, let);
                break;
            case CommandType.Call when cmd is AsCall call:
                await ExecuteCall(ctx, call);
                break;
            case CommandType.Return when cmd is AsReturn @return:
                await ExecuteReturn(@return);
                break;
            case CommandType.Param when cmd is AsParam param:
                await ExecuteParam(param);
                break;
            case CommandType.Comment:
                break;
            default:
                throw new UnknownCommandException(cmd);
        }
    }


    public ValueTask Run(CancellationToken token = default) {
        return Run(new AsExecutionContext(Logging, token));
    }

    public async ValueTask Run(AsExecutionContext ctx) {
        try
        {
            ctx.VariableContext = Variables;
            foreach (var cmd in Tree!.Commands)
            {
                await RunCommand(ctx, cmd);
            }
        }
        catch (Exception ex) 
        {
            throw new AsRuntimeException(ctx, ex);
        }
    }

    public void Dispose()
    {
        OnCommandUpdate = null;
        OnLogging = null;
    }

    public static AsInterpreter Of(VariableContext variableContext, AsAnalysis tree)
    {
        return new AsInterpreter(tree, variableContext);
    }

    public static AsInterpreter Of(VariableContext variableContext, string code)
    {
        return Of(variableContext, ScriptParser.Parse(LexicalAnalyzer.Analyze(code))!);
    }

    public static AsInterpreter Of(string code)
    {
        return Of(new VariableContext(), code);
    }
}