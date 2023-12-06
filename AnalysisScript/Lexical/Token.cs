namespace AnalysisScript.Lexical
{
    public interface IToken
    {
        public TokenType Type { get; }
        public int Pos { get; set; }
    }
    public interface IWordToken : IToken
    {
        public string Word { get; }
    }

    public static class Token
    {
        public record struct NewLine(int Pos) : IToken
        {
            public readonly TokenType Type => TokenType.NewLine;
        }
        public record struct Equal(int Pos) : IToken
        {
            public readonly TokenType Type => TokenType.Equal;
        }
        public record struct Pipe(int Pos) : IToken
        {
            public readonly TokenType Type => TokenType.Pipe;
        }
        public record struct Ui(int Pos) : IToken
        {
            public readonly TokenType Type => TokenType.Ui;
        }
        public record struct Let(int Pos) : IToken
        {
            public readonly TokenType Type => TokenType.Let;
        }
        public record struct Param(int Pos) : IToken
        {
            public readonly TokenType Type => TokenType.Param;
        }
        public record struct Return(int Pos) : IToken
        {
            public readonly TokenType Type => TokenType.Return;
        }
        public record struct Comment(string Word, int Pos) : IWordToken
        {
            public readonly TokenType Type => TokenType.Comment;
        }

        public record struct Identity(string Word, int Pos) : IWordToken
        {
            public readonly TokenType Type => TokenType.Identity;
        }
        public record struct String(string Word, int Pos) : IWordToken
        {
            public readonly TokenType Type => TokenType.String;
        }

        public record struct Number(double Real, int Pos) : IToken
        {
            public readonly TokenType Type => TokenType.Number;
        }
    }
}
