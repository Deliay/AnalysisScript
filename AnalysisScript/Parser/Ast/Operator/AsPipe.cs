using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Parser.Ast.Operator
{
    public class AsPipe(Token.Pipe lexical, AsIdentity fn, List<AsObject> args) : AsObject<Token.Pipe>(lexical)
    {
        public AsIdentity FunctionName => fn;
        public List<AsObject> Arguments => args;

        public override string ToString()
        {
            return $"| {FunctionName} {string.Join(' ', Arguments)}";
        }
    }
}
