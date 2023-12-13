namespace AnalysisScript.Lexical
{
    public enum TokenType
    {
        Equal = '=',
        Pipe = '|',
        Comment = '#',
        NewLine = '\n',
        END = '\xFF',
        Param,
        Let,
        Ui,
        Identity,
        String,
        Number,
        Integer,
        Return,
    }
}
