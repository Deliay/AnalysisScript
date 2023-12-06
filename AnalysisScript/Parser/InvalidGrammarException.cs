using AnalysisScript.Lexical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Parser
{
    public class InvalidGrammarException(IToken actual, params TokenType[] except)
        : Exception($"Invalid grammar: except {string.Join(',', except)} but actual is {actual.Type} in pos {actual.Pos}")
    {
    }
}
