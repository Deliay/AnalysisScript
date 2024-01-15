namespace AnalysisScript.Lexical;

public enum TokenType
{
    Equal = '=',
    Pipe = '|',
    Comment = '#',
    Reference = '&',
    ArrayStart = '[',
    ArrayEnd = ']',
    Comma = ',',
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