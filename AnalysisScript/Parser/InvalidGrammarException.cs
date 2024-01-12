using AnalysisScript.Lexical;

namespace AnalysisScript.Parser;

public class InvalidGrammarException(IToken actual, params TokenType[] except)
    : Exception($"Invalid grammar: except {string.Join(',', except)} but actual is {actual.Type} in pos {actual.Pos}")
{
}