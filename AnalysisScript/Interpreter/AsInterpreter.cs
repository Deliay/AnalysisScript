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
    private VariableContext Variables { get; } = new();
    public object? Return { get; private set; }
    public string LastComment { get; private set; } = "";
    public string CurrentCommand { get; private set; } = "";
    public AsExecutionContext Context { get; }

    public event Action<string>? OnCommentUpdate;

    public event Action<string>? OnCommandUpdate;

    public event Action<string>? OnLogging;

    public AsInterpreter()
    {
        this.Context = new AsExecutionContext(Logging);
    }

    private void Logging(string message)
    {
        OnLogging?.Invoke(message);
    }

    private async ValueTask<object> RunPipe(List<AsPipe> pipes, MethodCallExpression initialValue)
    {
        if (pipes.Count == 0) return initialValue;

        var value = initialValue;
        foreach (var pipe in pipes)
        {
            //var func = Methods[pipe.FunctionName.Name];
            //var args = pipe.Arguments.Select(ValueOf).ToArray();
            //value = await func(Context, value, args);

            var pipeValueGetter = Variables.GetMethodCallLambda(value, pipe.FunctionName.Name, pipe.Arguments, Context).Compile();
            var nextValue = await pipeValueGetter();

            value = Variables.LambdaValueOf(Variables.AddTempVar(nextValue, pipe));
        }

        return value;
    }

    private async ValueTask ExecuteLet(AsLet let)
    {
        var initValue = Variables.LambdaValueOf(let.Arg);
        this.Variables.PutVariable(let.Name, await RunPipe(let.Pipes, initValue));
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
        Return = Variables.__Boxed_GetVariable(@return.Variable);
        return ValueTask.CompletedTask;
    }

    private ValueTask ExecuteParam(AsParam param)
    {
        if (!Variables.HasVariable(param.Variable)) throw new UnknownVariableException(param.Variable);
        return ValueTask.CompletedTask;
    }

    public AsInterpreter RegisterFunction(string name, MethodInfo method)
    {
        Variables.Methods.RegisterFunction(name, method);
        return this;
    }

    public AsInterpreter AddVariable(string name, object value)
    {
        Variables.__Unsafe_UnBoxed_PutVariable(name, value);
        return this;
    }
    
    public async ValueTask<T?> RunAndReturn<T>(AsAnalysis tree, CancellationToken token)
    {
        await Run(tree, token);
        if (Return is not null)
        {
            Assert.Is<T>(Return, out var result);
            return result;
        }

        return default;
    }

    private async ValueTask RunCommand(AsCommand cmd, CancellationToken token = default)
    {
        CurrentCommand = cmd?.ToString()!;
        OnCommandUpdate?.Invoke(CurrentCommand);

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

    public async ValueTask Run(AsAnalysis tree, CancellationToken token = default) {
        foreach (var cmd in tree.Commands)
        {
            try
            {
                await RunCommand(cmd, token);
            }
            catch (Exception ex) 
            {
                throw new AsRuntimeException(cmd, ex);
            }
        }
    }

    public void Dispose()
    {
        OnCommandUpdate = null;
        OnCommentUpdate = null;
        OnLogging = null;
    }
}