using AnalysisScript.Lexical;

namespace AnalysisScript.Parser.Ast.Basic;

public class AsInteger(Token.Integer lexicalToken) : AsObject<Token.Integer>(lexicalToken)
{
    public int Value => LexicalToken.Value;

    public override string ToString()
    {
        return Value.ToString();
    }
}