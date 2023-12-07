using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast.Basic;

namespace AnalysisScript.Interpreter;

public class UnknownVariableException(IWordToken identity)
    : Exception($"Unknown variable {identity.Word} at pos {identity.Pos}")
{
    public UnknownVariableException(AsIdentity id) : this(id.LexicalToken)
    {
        
    }
}
