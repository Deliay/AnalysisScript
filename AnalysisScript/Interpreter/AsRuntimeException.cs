using AnalysisScript.Parser.Ast.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Interpreter
{
    public class AsRuntimeException(AsCommand cmd, Exception e) : Exception($"{e.Message} \n Execution failed at pos {cmd.LexicalToken.Pos}: {cmd}")
    {
    }
}
