using AnalysisScript.Lexical;

namespace AnalysisScript.Parser.Ast.Basic;

public class AsArray(Token.ArrayStart arrayStart, IReadOnlyList<AsObject> items) : AsObject<Token.ArrayStart>(arrayStart)
{
    public IReadOnlyList<AsObject> Items => items;
}