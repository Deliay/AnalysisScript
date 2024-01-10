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
    public class AsCall(
        Token.Call lexical,
        AsIdentity methodName,
        List<AsObject> args)
        : AsCommand<Token.Call>(lexical, CommandType.Call)
    {
        public AsIdentity Method => methodName;

        public List<AsObject> Args => args;

        public override string ToString()
        {
            return $"call {Method} {string.Join(' ', Args)}";
        }
    }
}
