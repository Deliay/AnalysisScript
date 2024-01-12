using AnalysisScript.Lexical;

namespace AnalysisScript.Parser.Ast.Basic;

public class AsNumber(Token.Number lexicalToken) : AsObject<Token.Number>(lexicalToken)
{
    public double Real => LexicalToken.Real;

    public override string ToString()
    {
        return Real.ToString();
    }
}