using AnalysisScript.Lexical;

namespace AnalysisScript.Parser.Ast.Basic;

public class AsIdentity(Token.Identity lexicalToken) : AsObject<Token.Identity>(lexicalToken)
{
    public string Name => LexicalToken.Word;

    public override string ToString()
    {
        return Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is AsIdentity target)
            return target.Name == Name;
        else
            return ReferenceEquals(this, obj);
    }

    public static readonly AsIdentity Reference = new(new Token.Identity("&", 0, 0));
}