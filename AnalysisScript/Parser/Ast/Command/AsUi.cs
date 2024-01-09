using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast.Basic;
using AnalysisScript.Parser.Ast.Operator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Parser.Ast.Command
{
    public class AsUi(
        Token.Call lexical,
        List<AsPipe> pipes)
        : AsCommand<Token.Call>(lexical, CommandType.Ui)
    {
        public List<AsPipe> Pipes => pipes;

        public override string ToString()
        {
            return $"ui\n{string.Join('\n', Pipes)}";
        }
    }
}
