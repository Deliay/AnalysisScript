using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast.Basic;

namespace AnalysisScript.Parser.Ast.Command;

public class AsCall(
    Token.Call lexical,
    AsIdentity methodName,
    List<AsObject> args)
    : AsCommand<Token.Call>(lexical, CommandType.Call)
{
    public AsIdentity Method => methodName;

    public List<AsObject> Args => args;

    public override string ToString()
    {
        return $"call {Method} {string.Join(' ', Args)}";
    }
}