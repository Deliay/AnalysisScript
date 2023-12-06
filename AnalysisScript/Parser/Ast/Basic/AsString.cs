using AnalysisScript.Lexical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Parser.Ast.Basic
{
    public class AsString(Token.String lexicalToken) : AsObject<Token.String>(lexicalToken)
    {
        public string RawContent { get; } = lexicalToken.Word;
    }
}
