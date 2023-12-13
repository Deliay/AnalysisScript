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
    public AsExecutionContext Context { get; }
    public AsAnalysis Tree { get; }

    public event Action<string>? OnCommentUpdate;

    public event Action<string>? OnCommandUpdate;

    public event Action<string>? OnLogging;

    public AsInterpreter(AsAnalysis tree)
    {
        this.Context = new AsExecutionContext(Logging);
        Tree = tree;
    }

    private void Logging(string message)
    {
        OnLogging?.Invoke(message);
    }

    private async ValueTask<AsIdentity> RunPipe(List<AsPipe> pipes, AsObject initialValue)
    {
        var initialValueId = Variables.Storage(initialValue);
        if (pipes.Count == 0) return initialValueId;

        var initValue = Variables.LambdaValueOf(initialValueId);
        var value = initValue;

        var lastValueId = initialValueId;
        foreach (var pipe in pipes)
        {
            Context.CurrentExecuteObject = pipe;

            var pipeValueGetter = Variables.GetMethodCallLambda(value, pipe.FunctionName.Name, pipe.Arguments, Context).Compile();
            var nextValue = await pipeValueGetter();
            var sanitizedValue = await ExprTreeHelper.SanitizeLambdaExpression(nextValue);
            
            value = Variables.LambdaValueOf(lastValueId = Variables.AddTempVar(sanitizedValue, pipe.LexicalToken));
        }

        return lastValueId;
    }

    private async ValueTask ExecuteLet(AsLet let)
    {
        var runtimeValue = await RunPipe(let.Pipes, let.Arg);
        this.Variables.PutVariableContainer(let.Name, Variables.GetVariableContainer(runtimeValue));
    }

    private ValueTask ExecuteUi(AsUi ui)
    {
        throw new NotImplementedException($"ui keyword not supported, pos {ui.LexicalToken.Pos}");
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
    
    public async ValueTask<T?> RunAndReturn<T>(CancellationToken token)
    {
        await Run(token);
        if (Return is not null)
        {
            return Return.ValueCastTo<T>();
        }

        return default;
    }

    private async ValueTask RunCommand(AsCommand cmd, CancellationToken token = default)
    {
        CurrentCommand = cmd?.ToString()!;
        OnCommandUpdate?.Invoke(CurrentCommand);

        Context.CurrentExecuteObject = cmd;

        if (cmd.Type == CommandType.Comment && cmd is AsComment comment)
        {
            await ExecuteComment(comment);
        }
        else if (cmd.Type == CommandType.Let && cmd is AsLet let)
        {
            await ExecuteLet(let);
        }
        else if (cmd.Type == CommandType.Ui && cmd is AsUi ui)
        {
            await ExecuteUi(ui);
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

    public async ValueTask Run(CancellationToken token = default) {
        try
        {
            foreach (var cmd in Tree.Commands)
            {
                await RunCommand(cmd, token);
            }
        }
        catch (Exception ex) 
        {
            throw new AsRuntimeException(Context, ex);
        }
    }

    public void Dispose()
    {
        OnCommandUpdate = null;
        OnCommentUpdate = null;
        OnLogging = null;
    }
}