using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast.Basic;

namespace AnalysisScript;

public class VariableAlreadyExistsException(AsIdentity identity)
    : Exception($"variable {identity.Name} at pos {identity.LexicalToken.Pos} already defined")
{

}
