namespace AnalysisScript.Lexical
{
    public class UnknownTokenException(int pos, string slice) : Exception($"Unknown token: {slice} at pos {pos}")
    {
    }
}
