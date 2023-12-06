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
    public class AsLet(
        Token.Let lexical,
        AsIdentity name,
        AsObject arg,
        List<AsPipe> pipes)
        : AsCommand<Token.Let>(lexical, CommandType.Let)
    {
        public AsIdentity Name => name;

        public AsObject Arg => arg;

        public List<AsPipe> Pipes => pipes;
    }
}
