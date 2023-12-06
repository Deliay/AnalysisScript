using AnalysisScript.Lexical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Parser.Ast.Command
{
    public class AsComment(Token.Comment lexical) : AsCommand<Token.Comment>(lexical, CommandType.Comment)
    {
        public string Content => LexicalToken.Word;

    }
}
