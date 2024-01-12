using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast.Basic;
using AnalysisScript.Parser.Ast.Operator;

namespace AnalysisScript.Parser.Ast.Command;

public class AsLet(
    Token.Let lexical,
    AsIdentity name,
    AsObject arg,
    List<AsPipe> pipes)
    : AsCommand<Token.Let>(lexical, CommandType.Let)
{
    public AsIdentity Name => name;

    public AsObject Arg => arg;

    public List<AsPipe> Pipes => pipes;

    public override string ToString()
    {
        return $"let {Name} = {Arg} \n{string.Join('\n', Pipes)}";
    }
}