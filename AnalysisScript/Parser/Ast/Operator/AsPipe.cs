using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast.Basic;

namespace AnalysisScript.Parser.Ast.Operator;

public class AsPipe(Token.Pipe lexical, AsIdentity fn, List<AsObject> args) : AsObject<Token.Pipe>(lexical)
{
    public AsIdentity FunctionName => fn;
    public List<AsObject> Arguments => args;

    public override string ToString()
    {
        return $"| {FunctionName} {string.Join(' ', Arguments)}";
    }
}