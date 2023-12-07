using AnalysisScript.Parser.Ast;
using AnalysisScript.Parser.Ast.Basic;
using AnalysisScript.Parser.Ast.Command;
using AnalysisScript.Parser.Ast.Operator;

namespace AnalysisScript.Interpreter;

public class AsInterpreter
{
    private Dictionary<string, object> Variables { get; } = [];
    private Dictionary<string, Func<object, object[], ValueTask<object>>> Methods { get; } = [];
    public object? Return { get; private set; }
    public string LastComment { get; private set; }
    public string CurrentCommand { get; private set; }

    public event Action<string> OnCommentUpdate;

    public event Action<string> OnCommandUpdate;
    
    private bool HasVariable(AsIdentity id) => Variables.ContainsKey(id.Name);

    private object GetVariable(AsIdentity id)
    {
        if (Variables.TryGetValue(id.Name, out var value)) return value;
        throw new UnknownVariableException(id);
    }

    private void PutVariable(AsIdentity id, object value)
    {
        if (Variables.ContainsKey(id.Name)) 
            throw new VariableAlreadyExistsException(id);

        Variables.Add(id.Name, value);
    }

    private string ParseString(AsString str)
    {
        var currentStr = str.RawContent;
        List<string> slices = [];
        int pos = 0;
        int left = currentStr.IndexOf("${");
        int right = currentStr.IndexOf('}');

        while (left > -1 && right > -1)
        {
            var varName = currentStr[(left + 2)..right];
            if (!Variables.TryGetValue(varName, out var value))
                throw new UnknownVariableException(str.LexicalToken);

            slices.Add(currentStr[pos..left]);
            slices.Add(value?.ToString() ?? "(null)");
            pos = right + 1;
            left = currentStr.IndexOf("${", pos);
            if (left == -1) break;
            right = currentStr.IndexOf('}', left);
        }
        slices.Add(currentStr[pos..]);

        return string.Join("", slices);
    }

    private object ValueOf(AsObject @object)
    {
        if (@object is AsString str) return ParseString(str);
        else if (@object is AsNumber num) return num.Real;
        else if (@object is AsIdentity id) return GetVariable(id);
        throw new UnknownValueObjectException(@object);
    }

    private async ValueTask<object> RunPipe(List<AsPipe> pipes, object initialValue)
    {
        if (pipes.Count == 0) return initialValue;

        var value = initialValue;
        foreach (var pipe in pipes)
        {
            var func = Methods[pipe.FunctionName.Name];
            var args = pipe.Arguments.Select(ValueOf).ToArray();
            value = await func(value, args);
        }

        return value;
    }

    private async ValueTask ExecuteLet(AsLet let)
    {
        var initValue = ValueOf(let.Arg);
        PutVariable(let.Name, await RunPipe(let.Pipes, initValue));
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
        Return = Variables[@return.Variable.Name];
        return ValueTask.CompletedTask;
    }

    private ValueTask ExecuteParam(AsParam param)
    {
        if (!HasVariable(param.Variable)) throw new UnknownVariableException(param.Variable);
        return ValueTask.CompletedTask;
    }

    public AsInterpreter RegisterFunction(string name, Func<object, object[], ValueTask<object>> func)
    {
        Methods.Add(name, func);
        return this;
    }

    public AsInterpreter AddVariable(string name, string value)
    {
        Variables.Add(name, value);
        return this;
    }
    

    public async ValueTask Run(AsAnalysis tree, CancellationToken token) {
        foreach (var cmd in tree.Commands)
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
    }

}