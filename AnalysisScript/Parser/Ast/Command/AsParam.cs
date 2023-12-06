using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Parser.Ast.Command
{
    public class AsParam(Token.Param lexicalToken, AsIdentity variable)
        : AsCommand<Token.Param>(lexicalToken, CommandType.Param)
    {
        public AsIdentity Variable => variable;

        public override string ToString()
        {
            return $"param {Variable}";
        }
    }
}
