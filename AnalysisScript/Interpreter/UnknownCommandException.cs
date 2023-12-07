using AnalysisScript.Parser.Ast.Command;

namespace AnalysisScript;

public class UnknownCommandException(AsCommand command)
    : Exception($"Unknown command: {command.Type} | {command.LexicalToken.Type} as pos {command.LexicalToken.Pos}")
{

}
