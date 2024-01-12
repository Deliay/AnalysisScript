using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast.Basic;

namespace AnalysisScript.Parser.Ast.Command;

public class AsReturn(Token.Return lexical, AsIdentity variable)
    : AsCommand<Token.Return>(lexical, CommandType.Return)
{
    public AsIdentity Variable => variable;

    public override string ToString()
    {
        return $"return {Variable}";
    }
}