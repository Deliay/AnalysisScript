using AnalysisScript.Lexical;

namespace AnalysisScript.Parser.Ast.Command;

public class AsComment(Token.Comment lexical) : AsCommand<Token.Comment>(lexical, CommandType.Comment)
{
    public string Content => LexicalToken.Word;

    public override string ToString()
    {
        return $"#{Content}";
    }
}