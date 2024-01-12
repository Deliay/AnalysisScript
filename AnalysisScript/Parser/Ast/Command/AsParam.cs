using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast.Basic;

namespace AnalysisScript.Parser.Ast.Command;

public class AsParam(Token.Param lexicalToken, AsIdentity variable)
    : AsCommand<Token.Param>(lexicalToken, CommandType.Param)
{
    public AsIdentity Variable => variable;

    public override string ToString()
    {
        return $"param {Variable}";
    }
}