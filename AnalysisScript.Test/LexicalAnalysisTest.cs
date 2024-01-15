namespace AnalysisScript.Test;

using Lexical;

public class LexicalAnalysisTest
{
    [Fact]
    public void DontSkipLf()
    { 
        var tokens = LexicalAnalyzer.Analyze("\n|\n");
        var collection = tokens.ToList();
        Assert.All(collection, (token, i) =>
        {
            switch (i)
            {
                case 0:
                case 2:
                    Assert.Equal(TokenType.NewLine, token.Type);
                    break;
                case 1:
                    Assert.Equal(TokenType.Pipe, token.Type);
                    break;
            }
        });
        Assert.True(collection.Count > 2);
    }
    [Fact]
    public void CanResolvePipe()
    {
        var tokens = LexicalAnalyzer.Analyze("|");
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.Pipe);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveArrayStart()
    {
        var tokens = LexicalAnalyzer.Analyze("[");
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.ArrayStart);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveArrayEnd()
    {
        var tokens = LexicalAnalyzer.Analyze("]");
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.ArrayEnd);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveComma()
    {
        var tokens = LexicalAnalyzer.Analyze(",");
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.Comma);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveRightArray()
    {
        var tokens = LexicalAnalyzer.Analyze("[a, b]");
        var lexicalTokens = tokens.ToList();
        Assert.Contains(lexicalTokens, token => token.Type == TokenType.ArrayStart);
        Assert.Contains(lexicalTokens, token => token.Type == TokenType.Identity);
        Assert.Contains(lexicalTokens, token => token.Type == TokenType.ArrayEnd);
        Assert.Equal(6, lexicalTokens.Count);
    }
    [Fact]
    public void CanResolveBlockPipe()
    {
        var tokens = LexicalAnalyzer.Analyze("||").ToList();
        Assert.Contains(tokens, token => token.Type == TokenType.Pipe);
        Assert.Equal(2, tokens.Count);
        Assert.True(Assert.Single(tokens.Where(t => t.Type == TokenType.Pipe).Cast<Token.Pipe>()).BlockSpread);
    }
    [Fact]
    public void CanResolveReference()
    {
        var tokens = LexicalAnalyzer.Analyze("&");
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.Reference);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveEqual()
    {
        var tokens = LexicalAnalyzer.Analyze("=");
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.Equal);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveRealNumber()
    {
        const double num = 1234.567;
        var tokens = LexicalAnalyzer.Analyze($"{num}");
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.Number);
        Assert.Equal(num, collection.Cast<Token.Number>().First().Real);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveIntegerNumber()
    {
        const int num = 1234;
        var tokens = LexicalAnalyzer.Analyze($"{num}");
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.Integer);
        Assert.Equal(num, collection.Cast<Token.Integer>().First().Value);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveIdentity()
    {
        const string id = "CanResolveIdentity";
        var tokens = LexicalAnalyzer.Analyze(id);
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.Identity);
        Assert.Equal(id, collection.Cast<Token.Identity>().First().Word);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolve_Identity()
    {
        const string id = "_CanResolve_Identity";
        var tokens = LexicalAnalyzer.Analyze(id);
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.Identity);
        Assert.Equal(id, collection.Cast<Token.Identity>().First().Word);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveLetIdentity()
    {
        const string id = "let";
        var tokens = LexicalAnalyzer.Analyze(id);
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.Let);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveParamIdentity()
    {
        const string id = "param";
        var tokens = LexicalAnalyzer.Analyze(id);
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.Param);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveCallIdentity()
    {
        const string id = "call";
        var tokens = LexicalAnalyzer.Analyze(id);
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.Call);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveReturnIdentity()
    {
        const string id = "return";
        var tokens = LexicalAnalyzer.Analyze(id);
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.Return);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveLiteralString()
    {
        const string id = "1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.String);
        Assert.Equal(id, collection.Cast<Token.String>().First().Word);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveLiteralStringQuoteEscape()
    {
        const string id = "\\\"1234";
        const string except = "\"1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.String);
        Assert.Equal(except, collection.Cast<Token.String>().First().Word);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveLiteralStringTabEscape()
    {
        const string id = "\\t1234";
        const string except = "\t1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.String);
        Assert.Equal(except, collection.Cast<Token.String>().First().Word);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveLiteralStringSpaceEscape()
    {
        const string id = "\\s1234";
        const string except = " 1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.String);
        Assert.Equal(except, collection.Cast<Token.String>().First().Word);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveLiteralStringBackspaceEscape()
    {
        const string id = "\\b1234";
        const string except = "\b1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.String);
        Assert.Equal(except, collection.Cast<Token.String>().First().Word);
        if (collection != null) Assert.Equal(2, collection.Count());
    }
    [Fact]
    public void CanResolveLiteralStringLfEscape()
    {
        const string id = "\\n1234";
        const string except = "\n1234";
        var tokens = LexicalAnalyzer.Analyze($"\"{id}\"");
        var collection = tokens.ToList();
        Assert.Contains(collection, token => token.Type == TokenType.String);
        Assert.Equal(except, collection.Cast<Token.String>().First().Word);
        Assert.Equal(2, collection.Count);
    }
    [Fact]
    public void CanResolveComment()
    {
        const string comment = " 114514";
        var tokens = LexicalAnalyzer.Analyze($"#{comment}");
        var resolvedTokenList = tokens.ToList();
        Assert.Contains(resolvedTokenList, token => token.Type == TokenType.Comment);
        Assert.Equal(comment, resolvedTokenList.Cast<Token.Comment>().First().Word);
        Assert.Equal(3, resolvedTokenList.Count);
    }
    [Fact]
    public void CanSkipSpace()
    {
        var tokens = LexicalAnalyzer.Analyze("|            \"123\"\n#123\n123");
        Assert.All(tokens, (token, i) =>
        {
            switch (i)
            {
                case 0:
                    Assert.Equal(TokenType.Pipe, token.Type);
                    break;
                case 1:
                    Assert.Equal(TokenType.String, token.Type);
                    break;
                case 2:
                    Assert.Equal(TokenType.NewLine, token.Type);
                    break;
                case 3:
                    Assert.Equal(TokenType.Comment, token.Type);
                    break;
                case 4:
                    Assert.Equal(TokenType.NewLine, token.Type);
                    break;
                case 5:
                    Assert.Equal(TokenType.Integer, token.Type);
                    break;
            }
        });
    }
    [Fact]
    public void ThrowWhenTokenInvalid()
    {
        const string id = "!";
        Assert.Throws<InvalidTokenException>(() => LexicalAnalyzer.Analyze(id).ToList());
    }
    [Fact]
    public void ThrowWhenStringEscapeInvalid()
    {
        const string id = "\"1234\\z\"";
        Assert.Throws<InvalidTokenException>(() => LexicalAnalyzer.Analyze(id).ToList());
    }
}