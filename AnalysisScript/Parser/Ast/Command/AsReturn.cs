using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Parser.Ast.Command
{
    public class AsReturn(Token.Return lexical, AsIdentity variable)
        : AsCommand<Token.Return>(lexical, CommandType.Return)
    {
        public AsIdentity Variable => variable;

        public override string ToString()
        {
            return $"return {Variable}";
        }
    }
}
