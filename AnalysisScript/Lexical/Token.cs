﻿namespace AnalysisScript.Lexical
{
    public interface IToken
    {
        public TokenType Type { get; }
        public int Pos { get; set; }
        public bool IsConstant { get; }
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
            public bool IsConstant { get; } = false;
        }
        public record struct Equal(int Pos) : IToken
        {
            public readonly TokenType Type => TokenType.Equal;
            public bool IsConstant { get; } = false;
        }
        public record struct Pipe(int Pos) : IToken
        {
            public readonly TokenType Type => TokenType.Pipe;
            public bool IsConstant { get; } = false;
        }
        public record struct Ui(int Pos) : IToken
        {
            public readonly TokenType Type => TokenType.Ui;
            public bool IsConstant { get; } = false;
        }
        public record struct Let(int Pos) : IToken
        {
            public readonly TokenType Type => TokenType.Let;
            public bool IsConstant { get; } = false;
        }
        public record struct Param(int Pos) : IToken
        {
            public readonly TokenType Type => TokenType.Param;
            public bool IsConstant { get; } = false;
        }
        public record struct Return(int Pos) : IToken
        {
            public readonly TokenType Type => TokenType.Return;
            public bool IsConstant { get; } = false;
        }
        public record struct Comment(string Word, int Pos) : IWordToken
        {
            public readonly TokenType Type => TokenType.Comment;
            public bool IsConstant { get; } = false;
        }

        public record struct Identity(string Word, int Pos) : IWordToken
        {
            public readonly TokenType Type => TokenType.Identity;
            public bool IsConstant { get; } = false;
        }
        public record struct String(string Word, int Pos) : IWordToken
        {
            public readonly TokenType Type => TokenType.String;
            public bool IsConstant { get; } = true;
        }

        public record struct Number(double Real, int Pos) : IToken
        {
            public readonly TokenType Type => TokenType.Number;
            public bool IsConstant { get; } = true;
        }
    }
}
