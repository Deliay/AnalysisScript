namespace AnalysisScript.Lexical;

public enum TokenType
{
    NewLine = '\n',
    Comment = '#',
    Reference = '&',
    Star = '*',
    Comma = ',',
    Equal = '=',
    ArrayStart = '[',
    ArrayEnd = ']',
    Pipe = '|',
    Param,
    Let,
    Call,
    Identity,
    String,
    Number,
    Integer,
    Return,
}