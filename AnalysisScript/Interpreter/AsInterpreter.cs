using AnalysisScript.Interpreter.Variables;
using AnalysisScript.Parser.Ast;
using AnalysisScript.Parser.Ast.Basic;
using AnalysisScript.Parser.Ast.Command;
using AnalysisScript.Parser.Ast.Operator;
using System.Reflection;
using AnalysisScript.Lexical;
using AnalysisScript.Parser;

namespace AnalysisScript.Interpreter;

public class AsInterpreter(AsAnalysis tree, VariableContext variableContext) : IDisposable
{
    public VariableContext Variables { get; } = variableContext;
    public IContainer? Return { get; private set; }
    
    [Obsolete("Property removed since 1.0.3")]
    public string LastComment { get; private set; } = "";
    public string CurrentCommand { get; private set; } = "";
    public AsAnalysis? Tree { get; set; } = tree;

    [Obsolete("Event removed since 1.0.3")]
    public event Action<string>? OnCommentUpdate;

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

    private async ValueTask<AsIdentity> RunPipe(AsExecutionContext ctx, List<AsPipe> pipes, AsObject initialValue)
    {
        var initialValueId = Variables.Storage(initialValue);
        if (pipes.Count == 0) return initialValueId;

        var initValue = Variables.LambdaValueOf(initialValueId);
        var value = initValue;

        var lastValueId = initialValueId;
        foreach (var pipe in pipes)
        {
            ctx.CurrentExecuteObject = pipe;

            var pipeValueGetter = Variables.GetMethodCallLambda(value, pipe.FunctionName.Name, pipe.Arguments, ctx).Compile();
            var nextValue = await pipeValueGetter();
            var sanitizedValue = await ExprTreeHelper.SanitizeLambdaExpression(nextValue);
            
            value = Variables.LambdaValueOf(lastValueId = Variables.AddTempVar(sanitizedValue, pipe.LexicalToken));
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

        await method();
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
        Variables.PutInitVariable(new AsIdentity(new Lexical.Token.Identity(name, 0, 0)), value);
        return this;
    }
    
    public ValueTask<T?> RunAndReturn<T>(CancellationToken token)
    {
        return RunAndReturn<T>(new AsExecutionContext(Logging, token));
    }

    public async ValueTask<T?> RunAndReturn<T>(AsExecutionContext ctx)
    {
        await Run(ctx);
        return Return is not null ? Return.ValueCastTo<T>() : default;
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
            foreach (var cmd in Tree.Commands)
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
        OnCommentUpdate = null;
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