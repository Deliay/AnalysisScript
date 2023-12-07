using AnalysisScript.Parser.Ast.Basic;

namespace AnalysisScript.Interpreter;

public class UnknownValueObjectException(AsObject obj)
    : Exception($"Unknown value object: {obj.LexicalToken.Type} at {obj.LexicalToken.Pos}")
{

}
