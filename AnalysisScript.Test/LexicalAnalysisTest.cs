namespace AnalysisScript.Test;

using AnalysisScript.Lexical;

public class LexicalAnalysisTest
{
    [Fact]
    public void WillSkipLf()
    {
        var tokens = LexicalAnalyzer.Analyze("\n|\n");
        Assert.All(tokens, (token, i) =>
        {
            if (i == 0 || i == 2)
            {
                Assert.Equal(TokenType.NewLine, token.Type);
            }

            if (i == 1)
            {
                Assert.Equal(TokenType.Pipe, token.Type);
            }
        });
        Assert.True(tokens.Count() > 2);
    }
    [Fact]
    public void CanResolvePipeCorrectly()
    {
        var tokens = LexicalAnalyzer.Analyze("|");
        Assert.Contains(tokens, token => token.Type == TokenType.Pipe);
    }
    [Fact]
    public void CanResolveEqualCorrectly()
    {
        var tokens = LexicalAnalyzer.Analyze("=");
        Assert.Contains(tokens, token => token.Type == TokenType.Equal);
    }
    [Fact]
    public void CanResolveRealNumberCorrectly()
    {
        double num = 1234.567;
        var tokens = LexicalAnalyzer.Analyze($"{num}");
        Assert.Contains(tokens, token => token.Type == TokenType.Number);
        Assert.Equal(num, tokens.Cast<Token.Number>().First().Real);
    }
    [Fact]
    public void CanResolveIntegerNumberCorrectly()
    {
        int num = 1234;
        var tokens = LexicalAnalyzer.Analyze($"{num}");
        Assert.Contains(tokens, token => token.Type == TokenType.Integer);
        Assert.Equal(num, tokens.Cast<Token.Integer>().First().Value);
    }
    [Fact]
    public void CanResolveIdentityCorrectly()
    {
        string id = "CanResolveIdentityCorrectly";
        var tokens = LexicalAnalyzer.Analyze(id);
        Assert.Contains(tokens, token => token.Type == TokenType.Identity);
        Assert.Equal(id, tokens.Cast<Token.Identity>().First().Word);
    }
    [Fact]
    public void CanResolve_IdentityCorrectly()
    {
        string id = "_CanResolve_IdentityCorrectly";
        var tokens = LexicalAnalyzer.Analyze(id);
        Assert.Contains(tokens, token => token.Type == TokenType.Identity);
        Assert.Equal(id, tokens.Cast<Token.Identity>().First().Word);
    }
    [Fact]
    public void CanResolveLetIdentityCorrectly()
    {
        string id = "let";
        var tokens = LexicalAnalyzer.Analyze(id);
        Assert.Contains(tokens, token => token.Type == TokenType.Let);
    }
    [Fact]
    public void CanResolveParamIdentityCorrectly()
    {
        string id = "param";
        var tokens = LexicalAnalyzer.Analyze(id);
        Assert.Contains(tokens, token => token.Type == TokenType.Param);
    }
    [Fact]
    public void CanResolveCallIdentityCorrectly()
    {
        string id = "call";
        var tokens = LexicalAnalyzer.Analyze(id);
        Assert.Contains(tokens, token => token.Type == TokenType.Call);
    }
    [Fact]
    public void CanResolveReturnIdentityCorrectly()
    {
        string id = "return";
        var tokens = LexicalAnalyzer.Analyze(id);
        Assert.Contains(tokens, token => token.Type == TokenType.Return);
    }
    [Fact]
    public void CanResolveLiteralStringCorrectly()
    {
        string id = "1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        Assert.Contains(tokens, token => token.Type == TokenType.String);
        Assert.Equal(id, tokens.Cast<Token.String>().First().Word);
    }
    [Fact]
    public void CanResolveLiteralStringQuoteEscapeCorrectly()
    {
        string id = "\\\"1234";
        string except = "\"1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        Assert.Contains(tokens, token => token.Type == TokenType.String);
        Assert.Equal(except, tokens.Cast<Token.String>().First().Word);
    }
    [Fact]
    public void CanResolveLiteralStringTabEscapeCorrectly()
    {
        string id = "\\t1234";
        string except = "\t1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        Assert.Contains(tokens, token => token.Type == TokenType.String);
        Assert.Equal(except, tokens.Cast<Token.String>().First().Word);
    }
    [Fact]
    public void CanResolveLiteralStringSpaceEscapeCorrectly()
    {
        string id = "\\s1234";
        string except = " 1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        Assert.Contains(tokens, token => token.Type == TokenType.String);
        Assert.Equal(except, tokens.Cast<Token.String>().First().Word);
    }
    [Fact]
    public void CanResolveLiteralStringBackspaceEscapeCorrectly()
    {
        string id = "\\b1234";
        string except = "\b1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        Assert.Contains(tokens, token => token.Type == TokenType.String);
        Assert.Equal(except, tokens.Cast<Token.String>().First().Word);
    }
    [Fact]
    public void CanResolveLiteralStringLfEscapeCorrectly()
    {
        string id = "\\n1234";
        string except = "\n1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        Assert.Contains(tokens, token => token.Type == TokenType.String);
        Assert.Equal(except, tokens.Cast<Token.String>().First().Word);
    }
    [Fact]
    public void CanResolveCommentCorrectly()
    {
        string comment = " 114514";
        var tokens = LexicalAnalyzer.Analyze($"#{comment}");
        Assert.Contains(tokens, token => token.Type == TokenType.Comment);
        Assert.Equal(comment, tokens.Cast<Token.Comment>().First().Word);
    }
    [Fact]
    public void CanSkipSpaceCorrectly()
    {
        var tokens = LexicalAnalyzer.Analyze("|            \"123\"\n#123\n123");
        Assert.All(tokens, (token, i) =>
        {
            if (i == 0) Assert.Equal(TokenType.Pipe, token.Type);
            if (i == 1) Assert.Equal(TokenType.String, token.Type);
            if (i == 2) Assert.Equal(TokenType.NewLine, token.Type);
            if (i == 3) Assert.Equal(TokenType.Comment, token.Type);
            if (i == 4) Assert.Equal(TokenType.NewLine, token.Type);
            if (i == 5) Assert.Equal(TokenType.Integer, token.Type);
        });
    }
    [Fact]
    public void ThrowWhenTokenInvalid()
    {
        string id = "!";
        Assert.Throws<InvalidTokenException>(() => LexicalAnalyzer.Analyze(id).ToList());
    }
    [Fact]
    public void ThrowWhenStringEscapeInvalid()
    {
        string id = "\"1234\\z\"";
        Assert.Throws<InvalidTokenException>(() => LexicalAnalyzer.Analyze(id).ToList());
    }
}