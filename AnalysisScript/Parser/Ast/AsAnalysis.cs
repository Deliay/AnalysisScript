using AnalysisScript.Parser.Ast.Command;

namespace AnalysisScript.Parser.Ast;

public class AsAnalysis(List<AsCommand> commands)
{
    public List<AsCommand> Commands => commands;

    public override string ToString()
    {
        return string.Join('\n', Commands);
    }
}