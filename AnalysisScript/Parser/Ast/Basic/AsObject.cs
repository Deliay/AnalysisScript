using AnalysisScript.Lexical;

namespace AnalysisScript.Parser.Ast.Basic;

public abstract class AsObject(IToken lexicalToken)
{
    public IToken LexicalToken => lexicalToken;
}

public abstract class AsObject<T>(T lexicalToken) : AsObject(lexicalToken) where T : IToken
{
    public new T LexicalToken => lexicalToken;
}