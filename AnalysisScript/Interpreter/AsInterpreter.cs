using AnalysisScript.Interpreter.Variables;
using AnalysisScript.Library;
using AnalysisScript.Parser.Ast;
using AnalysisScript.Parser.Ast.Basic;
using AnalysisScript.Parser.Ast.Command;
using AnalysisScript.Parser.Ast.Operator;
using System.Linq.Expressions;
using System.Reflection;

namespace AnalysisScript.Interpreter;

public class AsInterpreter : IDisposable
{
    public VariableContext Variables { get; } = new();
    public IContainer? Return { get; private set; }
    public string LastComment { get; private set; } = "";
    public string CurrentCommand { get; private set; } = "";
    public AsAnalysis Tree { get; }

    public event Action<string>? OnCommentUpdate;

    public event Action<string>? OnCommandUpdate;

    public event Action<string>? OnLogging;

    public AsInterpreter(AsAnalysis tree)
    {
        Tree = tree;
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

    private ValueTask ExecuteComment(AsComment comment)
    {
        LastComment = comment.Content;
        OnCommentUpdate?.Invoke(LastComment);
        return ValueTask.CompletedTask;
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

    public AsInterpreter RegisterStaticFunction(string name, MethodInfo method)
    {
        Variables.Methods.RegisterStaticFunction(name, method);
        return this;
    }
    
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
        if (Return is not null)
        {
            return Return.ValueCastTo<T>();
        }

        return default;
    }

    private async ValueTask RunCommand(AsExecutionContext ctx, AsCommand cmd)
    {
        CurrentCommand = cmd.ToString()!;
        OnCommandUpdate?.Invoke(CurrentCommand);

        ctx.CurrentExecuteObject = cmd;

        if (cmd.Type == CommandType.Comment && cmd is AsComment comment)
        {
            await ExecuteComment(comment);
        }
        else if (cmd.Type == CommandType.Let && cmd is AsLet let)
        {
            await ExecuteLet(ctx, let);
        }
        else if (cmd.Type == CommandType.Call && cmd is AsCall call)
        {
            await ExecuteCall(ctx, call);
        }
        else if (cmd.Type == CommandType.Return && cmd is AsReturn @return)
        {
            await ExecuteReturn(@return);
        }
        else if (cmd.Type == CommandType.Param && cmd is AsParam param)
        {
            await ExecuteParam(param);
        }
        else throw new UnknownCommandException(cmd);
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
}