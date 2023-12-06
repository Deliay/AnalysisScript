using AnalysisScript.Parser.Ast.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Parser.Ast
{
    public class AsAnalysis(List<AsCommand> commands)
    {
        public List<AsCommand> Commands => commands;

        public override string ToString()
        {
            return string.Join('\n', Commands);
        }
    }
}
