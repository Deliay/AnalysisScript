using AnalysisScript.Lexical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Parser.Ast.Basic
{
    public class AsNumber(Token.Number lexicalToken) : AsObject<Token.Number>(lexicalToken)
    {
        public double Real => LexicalToken.Real;

        public override string ToString()
        {
            return Real.ToString();
        }
    }
}
