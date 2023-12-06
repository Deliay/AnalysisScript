using AnalysisScript.Lexical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Parser.Ast.Basic
{
    public class AsIdentity(Token.Identity lexicalToken) : AsObject<Token.Identity>(lexicalToken)
    {
        public string Name => LexicalToken.Word;

        public override string ToString()
        {
            return Name;
        }
    }
}
