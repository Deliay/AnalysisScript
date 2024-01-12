namespace AnalysisScript.Lexical;

public class InvalidTokenException(int pos, string slice) : Exception($"Unknown token: {slice} at pos {pos}")
{
}