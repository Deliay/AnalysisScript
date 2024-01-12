using AnalysisScript.Lexical;

namespace AnalysisScript.Parser;

public class ParserEndOfFileException(params TokenType[] types)
    : Exception($"Parser require {string.Join(',', types)} token but end of file")
{
}