namespace AnalysisScript.Test;

using AnalysisScript.Lexical;

public class LexicalAnalysisTest
{
    [Fact]
    public void DontSkipLf()
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
    public void CanResolvePipe()
    {
        var tokens = LexicalAnalyzer.Analyze("|");
        Assert.Contains(tokens, token => token.Type == TokenType.Pipe);
        Assert.Equal(2, tokens.Count());
    }
    [Fact]
    public void CanResolveEqual()
    {
        var tokens = LexicalAnalyzer.Analyze("=");
        Assert.Contains(tokens, token => token.Type == TokenType.Equal);
        Assert.Equal(2, tokens.Count());
    }
    [Fact]
    public void CanResolveRealNumber()
    {
        double num = 1234.567;
        var tokens = LexicalAnalyzer.Analyze($"{num}");
        Assert.Contains(tokens, token => token.Type == TokenType.Number);
        Assert.Equal(num, tokens.Cast<Token.Number>().First().Real);
        Assert.Equal(2, tokens.Count());
    }
    [Fact]
    public void CanResolveIntegerNumber()
    {
        int num = 1234;
        var tokens = LexicalAnalyzer.Analyze($"{num}");
        Assert.Contains(tokens, token => token.Type == TokenType.Integer);
        Assert.Equal(num, tokens.Cast<Token.Integer>().First().Value);
        Assert.Equal(2, tokens.Count());
    }
    [Fact]
    public void CanResolveIdentity()
    {
        string id = "CanResolveIdentity";
        var tokens = LexicalAnalyzer.Analyze(id);
        Assert.Contains(tokens, token => token.Type == TokenType.Identity);
        Assert.Equal(id, tokens.Cast<Token.Identity>().First().Word);
        Assert.Equal(2, tokens.Count());
    }
    [Fact]
    public void CanResolve_Identity()
    {
        string id = "_CanResolve_Identity";
        var tokens = LexicalAnalyzer.Analyze(id);
        Assert.Contains(tokens, token => token.Type == TokenType.Identity);
        Assert.Equal(id, tokens.Cast<Token.Identity>().First().Word);
        Assert.Equal(2, tokens.Count());
    }
    [Fact]
    public void CanResolveLetIdentity()
    {
        string id = "let";
        var tokens = LexicalAnalyzer.Analyze(id);
        Assert.Contains(tokens, token => token.Type == TokenType.Let);
        Assert.Equal(2, tokens.Count());
    }
    [Fact]
    public void CanResolveParamIdentity()
    {
        string id = "param";
        var tokens = LexicalAnalyzer.Analyze(id);
        Assert.Contains(tokens, token => token.Type == TokenType.Param);
        Assert.Equal(2, tokens.Count());
    }
    [Fact]
    public void CanResolveCallIdentity()
    {
        string id = "call";
        var tokens = LexicalAnalyzer.Analyze(id);
        Assert.Contains(tokens, token => token.Type == TokenType.Call);
        Assert.Equal(2, tokens.Count());
    }
    [Fact]
    public void CanResolveReturnIdentity()
    {
        string id = "return";
        var tokens = LexicalAnalyzer.Analyze(id);
        Assert.Contains(tokens, token => token.Type == TokenType.Return);
        Assert.Equal(2, tokens.Count());
    }
    [Fact]
    public void CanResolveLiteralString()
    {
        string id = "1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        Assert.Contains(tokens, token => token.Type == TokenType.String);
        Assert.Equal(id, tokens.Cast<Token.String>().First().Word);
        Assert.Equal(2, tokens.Count());
    }
    [Fact]
    public void CanResolveLiteralStringQuoteEscape()
    {
        string id = "\\\"1234";
        string except = "\"1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        Assert.Contains(tokens, token => token.Type == TokenType.String);
        Assert.Equal(except, tokens.Cast<Token.String>().First().Word);
        Assert.Equal(2, tokens.Count());
    }
    [Fact]
    public void CanResolveLiteralStringTabEscape()
    {
        string id = "\\t1234";
        string except = "\t1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        Assert.Contains(tokens, token => token.Type == TokenType.String);
        Assert.Equal(except, tokens.Cast<Token.String>().First().Word);
        Assert.Equal(2, tokens.Count());
    }
    [Fact]
    public void CanResolveLiteralStringSpaceEscape()
    {
        string id = "\\s1234";
        string except = " 1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        Assert.Contains(tokens, token => token.Type == TokenType.String);
        Assert.Equal(except, tokens.Cast<Token.String>().First().Word);
        Assert.Equal(2, tokens.Count());
    }
    [Fact]
    public void CanResolveLiteralStringBackspaceEscape()
    {
        string id = "\\b1234";
        string except = "\b1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        Assert.Contains(tokens, token => token.Type == TokenType.String);
        Assert.Equal(except, tokens.Cast<Token.String>().First().Word);
        Assert.Equal(2, tokens.Count());
    }
    [Fact]
    public void CanResolveLiteralStringLfEscape()
    {
        string id = "\\n1234";
        string except = "\n1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        Assert.Contains(tokens, token => token.Type == TokenType.String);
        Assert.Equal(except, tokens.Cast<Token.String>().First().Word);
        Assert.Equal(2, tokens.Count());
    }
    [Fact]
    public void CanResolveComment()
    {
        string comment = " 114514";
        var tokens = LexicalAnalyzer.Analyze($"#{comment}");
        Assert.Contains(tokens, token => token.Type == TokenType.Comment);
        Assert.Equal(comment, tokens.Cast<Token.Comment>().First().Word);
        Assert.Equal(3, tokens.Count());
    }
    [Fact]
    public void CanSkipSpace()
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