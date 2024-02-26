namespace AnalysisScript.Lexical;

public class InvalidTokenException(int pos, int line, string slice) : Exception($"Unknown token: {slice} at pos {pos}")
{
    public int Line => line;
    public string Slice => slice;
}