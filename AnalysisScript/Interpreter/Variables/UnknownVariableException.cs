using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast.Basic;

namespace AnalysisScript.Interpreter.Variables;

public class UnknownVariableException(string token, IToken identity)
    : Exception($"Unknown variable {token} at pos {identity.Pos}, line {identity.Line}")
{
    public UnknownVariableException(AsIdentity id) : this(id.LexicalToken)
    {
    }

    public UnknownVariableException(IWordToken identity) : this(identity.Word, identity)
    {
    }
}