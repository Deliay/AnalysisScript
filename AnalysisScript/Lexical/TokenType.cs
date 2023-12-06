namespace AnalysisScript.Lexical
{
    public enum TokenType
    {
        Equal = '=',
        Pipe = '|',
        LineEnd = '\n',
        END = '\xFF',
        Param,
        Let,
        Ui,
        Identity,
        String,
        Number,
    }
}
