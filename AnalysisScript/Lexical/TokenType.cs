namespace AnalysisScript.Lexical;

public enum TokenType
{
    Equal = '=',
    Pipe = '|',
    Comment = '#',
    Reference = '&',
    NewLine = '\n',
    Param,
    Let,
    Call,
    Identity,
    String,
    Number,
    Integer,
    Return,
}