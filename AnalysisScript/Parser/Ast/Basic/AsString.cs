using AnalysisScript.Lexical;

namespace AnalysisScript.Parser.Ast.Basic;

public class AsString(Token.String lexicalToken) : AsObject<Token.String>(lexicalToken)
{
    public string RawContent { get; } = lexicalToken.Word;

    public override string ToString()
    {
        return $"\"{RawContent}\"";
    }
}