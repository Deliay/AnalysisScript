using AnalysisScript.Parser.Ast.Basic;

namespace AnalysisScript.Interpreter.Variables;

public class VariableAlreadyExistsException(string identity) : Exception(identity)
{
    public VariableAlreadyExistsException(AsIdentity identity)
        : this($"variable {identity.Name} at pos {identity.LexicalToken.Pos} already defined")
    {
    }
}