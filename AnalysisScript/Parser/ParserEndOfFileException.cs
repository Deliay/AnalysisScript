using AnalysisScript.Lexical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Parser
{
    public class ParserEndOfFileException(params TokenType[] types)
        : Exception($"Parser require {string.Join(',', types)} token but end of file")
    {
    }
}
