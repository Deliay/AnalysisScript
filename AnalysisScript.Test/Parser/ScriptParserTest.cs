namespace AnalysisScript.Test.Parser;

using Lexical;
using AnalysisScript.Parser;
using AnalysisScript.Parser.Ast.Basic;
using AnalysisScript.Parser.Ast.Command;

public class ScriptParserTest
{
    [Fact]
    public void CanParseLetKeyword()
    {
        const string varName = "a";
        const string varValue = "1";
        var ast = ScriptParser.Parse(LexicalAnalyzer.Analyze(
            $"""
                  let {varName} = "{varValue}"
                  """))!;

        var let = ast.Commands.Cast<AsLet>().First();
        Assert.Equal(varName, let.Name.Name);
        var arg = Assert.IsType<AsString>(let.Arg);
        Assert.Equal(varValue, arg.RawContent);
    }

    [Fact]
    public void CanParseLetKeywordWithNumberArg()
    {
        var varName = "a";
        var varValue = 1234.567;
        var ast = ScriptParser.Parse(LexicalAnalyzer.Analyze(
            $"""
                 let {varName} = {varValue}
                 """))!;

        var let = ast.Commands.Cast<AsLet>().First();
        Assert.Equal(varName, let.Name.Name);
        var arg = Assert.IsType<AsNumber>(let.Arg);
        Assert.Equal(varValue, arg.Real);
    }

    [Fact]
    public void CanParseLetKeywordWithIntegerArg()
    {
        var varName = "a";
        var varValue = 1234;
        var ast = ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
let {varName} = {varValue}
"))!;

        var let = ast.Commands.Cast<AsLet>().First();
        Assert.Equal(varName, let.Name.Name);
        var arg = Assert.IsType<AsInteger>(let.Arg);
        Assert.Equal(varValue, arg.Value);
    }

    [Fact]
    public void CanParseLetKeywordWithIdentityArg()
    {
        var varName = "a";
        var varValue = "b";
        var ast = ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
let {varName} = {varValue}
"))!;

        var let = ast.Commands.Cast<AsLet>().First();
        Assert.Equal(varName, let.Name.Name);
        var arg = Assert.IsType<AsIdentity>(let.Arg);
        Assert.Equal(varValue, arg.Name);
    }

    [Fact]
    public void ThrowWhenLetArgContinueWithOtherElement()
    {
        Assert.Throws<InvalidGrammarException>(() => ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
let a = let
")));
    }

    [Fact]
    public void CanParseLetKeywordWithPipe()
    {
        var varName = "a";
        var varValue = 1234.567;
        var tokens = LexicalAnalyzer.Analyze(@$"
let {varName} = {varValue}
| b
");
        var ast = ScriptParser.Parse(tokens)!;

        var let = ast.Commands.Cast<AsLet>().First();
        Assert.Equal(varName, let.Name.Name);
        var arg = Assert.IsType<AsNumber>(let.Arg);
        Assert.Equal(varValue, arg.Real);
        Assert.Single(let.Pipes);
    }

    [Fact]
    public void CanParseLetKeywordWithPipes()
    {
        var varName = "a";
        var varValue = 1234.567;
        var ast = ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
let {varName} = {varValue}
| b
| c
"))!;

        var let = ast.Commands.Cast<AsLet>().First();
        Assert.Equal(varName, let.Name.Name);
        var arg = Assert.IsType<AsNumber>(let.Arg);
        Assert.Equal(varValue, arg.Real);
        Assert.Equal(2, let.Pipes.Count);
    }

    [Fact]
    public void CanParseManyLetKeywordWithPipes()
    {
        var varName = "a";
        var varValue = 1234.567;
        var ast = ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
let {varName} = {varValue}
| b
| c
let {varName} = {varValue}
| b
| c
| d

let {varName} = {varValue}
| b
| c
"))!;

        Assert.Equal(3, ast.Commands.Count);

        ast.Commands[0].IsLet(
            id: (id) => Assert.Equal(varName, id.Name),
            arg: (arg) => arg.IsNumber(varValue),
            pipes: (pipes) => Assert.Equal(2, pipes.Count)
        );
        ast.Commands[1].IsLet(
            id: (id) => Assert.Equal(varName, id.Name),
            arg: (arg) => arg.IsNumber(varValue),
            pipes: (pipes) => Assert.Equal(3, pipes.Count)
        );
        ast.Commands[2].IsLet(
            id: (id) => Assert.Equal(varName, id.Name),
            arg: (arg) => arg.IsNumber(varValue),
            pipes: (pipes) => Assert.Equal(2, pipes.Count)
        );
    }

    [Fact]
    public void CanParseManyLetKeywordWithPipesAndManyArgs()
    {
        var varName = "a";
        var varValue = 1234.567;
        var ast = ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
let {varName} = {varValue}
| b e f
| c e f
let {varName} = {varValue}
| b e ""1"" f
| d
| c 1 e f

let {varName} = {varValue}
| b 1.1 ""1"" 1
| c e ""1"" 1.1 f
"))!;

        Assert.Equal(3, ast.Commands.Count);

        ast.Commands[0].IsLet(
            id: (id) => Assert.Equal(varName, id.Name),
            arg: (arg) => arg.IsNumber(varValue),
            pipes: (pipes) =>
            {
                Assert.Equal(2, pipes.Count);
                pipes[0].FunctionName.IsIdentity("b");
                pipes[0].Arguments[0].IsIdentity("e");
                pipes[0].Arguments[1].IsIdentity("f");
                pipes[1].FunctionName.IsIdentity("c");
                pipes[1].Arguments[0].IsIdentity("e");
                pipes[1].Arguments[1].IsIdentity("f");
            }
        );
        ast.Commands[1].IsLet(
            id: (id) => Assert.Equal(varName, id.Name),
            arg: (arg) => arg.IsNumber(varValue),
            pipes: (pipes) =>
            {
                Assert.Equal(3, pipes.Count);
                pipes[0].FunctionName.IsIdentity("b");
                pipes[0].Arguments[0].IsIdentity("e");
                pipes[0].Arguments[1].IsString("1");
                pipes[0].Arguments[2].IsIdentity("f");
                pipes[1].FunctionName.IsIdentity("d");
                Assert.Empty(pipes[1].Arguments);
                pipes[2].FunctionName.IsIdentity("c");
                pipes[2].Arguments[0].IsInteger(1);
                pipes[2].Arguments[1].IsIdentity("e");
                pipes[2].Arguments[2].IsIdentity("f");
            }
        );
        ast.Commands[2].IsLet(
            id: (id) => Assert.Equal(varName, id.Name),
            arg: (arg) => arg.IsNumber(varValue),
            pipes: (pipes) =>
            {
                Assert.Equal(2, pipes.Count);
                pipes[0].FunctionName.IsIdentity("b");
                pipes[0].Arguments[0].IsNumber(1.1);
                pipes[0].Arguments[1].IsString("1");
                pipes[0].Arguments[2].IsInteger(1);
                pipes[1].FunctionName.IsIdentity("c");
                pipes[1].Arguments[0].IsIdentity("e");
                pipes[1].Arguments[1].IsString("1");
                pipes[1].Arguments[2].IsNumber(1.1);
                pipes[1].Arguments[3].IsIdentity("f");
            }
        );
    }

    [Fact]
    public void CanParseManyLetKeywordWithPipesAndComments()
    {
        var varName = "a";
        var varValue = 1234.567;
        var ast = ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
# comment!
let {varName} = {varValue}
| b
# comment!
| c
# comment!
let {varName} = {varValue}
| b
# comment!
# comment!
# comment!
| c
# comment!
| d

# comment!
let {varName} = {varValue}
| b
| c
"))!;

        Assert.Equal(3, ast.Commands.Count);

        ast.Commands[0].IsLet(
            id: (id) => Assert.Equal(varName, id.Name),
            arg: (arg) => arg.IsNumber(varValue),
            pipes: (pipes) => Assert.Equal(2, pipes.Count)
        );
        ast.Commands[1].IsLet(
            id: (id) => Assert.Equal(varName, id.Name),
            arg: (arg) => arg.IsNumber(varValue),
            pipes: (pipes) => Assert.Equal(3, pipes.Count)
        );
        ast.Commands[2].IsLet(
            id: (id) => Assert.Equal(varName, id.Name),
            arg: (arg) => arg.IsNumber(varValue),
            pipes: (pipes) => Assert.Equal(2, pipes.Count)
        );
    }

    [Fact]
    public void CanParseCallKeywordWithArg()
    {
        var varName = "a";
        var varValue = 1234.567;
        var ast = ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
call {varName} {varValue}
"))!;
        Assert.Single(ast.Commands);
        ast.Commands[0].IsCall(
            id => id.IsIdentity(varName),
            args => Assert.Single(args).IsNumber(varValue)
        );
    }

    [Fact]
    public void CanParseCallKeywordWithManyArg()
    {
        var varName = "a";
        var varValue = 1234.567;
        var ast = ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
call {varName} {varValue} a b c 1.1 ""1""
"))!;
        Assert.Single(ast.Commands);
        ast.Commands[0].IsCall(
            id => id.IsIdentity(varName),
            args =>
            {
                Assert.Equal(6, args.Count);
                args[0].IsNumber(varValue);
                args[1].IsIdentity("a");
                args[2].IsIdentity("b");
                args[3].IsIdentity("c");
                args[4].IsNumber(1.1);
                args[5].IsString("1");
            }
        );
    }

    [Fact]
    public void CanParseCallKeywordWithoutArg()
    {
        var varName = "a";
        var ast = ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
call {varName}
"))!;
        Assert.Single(ast.Commands);
        ast.Commands[0].IsCall(
            id => id.IsIdentity(varName),
            Assert.Empty
        );
    }

    [Fact]
    public void CanParseReturnKeywordWithArg()
    {
        var varName = "a";
        var ast = ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
return {varName}
"))!;
        Assert.Single(ast.Commands);
        ast.Commands[0].IsReturn(
            id => id.IsIdentity(varName)
        );
    }

    [Fact]
    public void ThrowWhenReturnKeywordNotPassArg()
    {
        Assert.Throws<InvalidGrammarException>(() => ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
return
")));
    }

    [Fact]
    public void ThrowWhenReturnKeywordWithManyArgs()
    {
        Assert.Throws<InvalidGrammarException>(() => ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
return a b
")));
    }

    [Fact]
    public void CanParseParamKeywordWithArg()
    {
        var varName = "a";
        var ast = ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
param {varName}
"))!;
        Assert.Single(ast.Commands);
        ast.Commands[0].IsParam(
            id => id.IsIdentity(varName)
        );
    }

    [Fact]
    public void ThrowWhenParamKeywordNotPassArg()
    {
        Assert.Throws<InvalidGrammarException>(() => ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
param
")));
    }

    [Fact]
    public void ThrowWhenParamKeywordWithManyArgs()
    {
        Assert.Throws<InvalidGrammarException>(() => ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
param a b
")));
    }

    [Fact]
    public void AllGrammarTest()
    {
        ScriptParser.Parse(LexicalAnalyzer.Analyze(@$"
param a

let b = a
| c [1,2,3]
| d &
|| d & [[1,2], &]

call e f g

return d
"));
    }

    [Fact]
    public void CanParseReferenceIdentityInPipes()
    {
        var varName = "a";
        var varValue = 1234.567;
        var tokens = LexicalAnalyzer.Analyze(@$"
let {varName} = {varValue}
| c &
");
        var ast = ScriptParser.Parse(tokens)!;

        var let = ast.Commands.Cast<AsLet>().First();
        Assert.Equal(varName, let.Name.Name);
        var arg = Assert.IsType<AsNumber>(let.Arg);
        Assert.Equal(varValue, arg.Real);
        Assert.Single(Assert.Single(let.Pipes).Arguments).IsIdentity(AsIdentity.Reference.LexicalToken.Word);
    }
    
    [Fact]
    public void CanParseArrayInLetParams()
    {
        var varName = "a";
        var tokens = LexicalAnalyzer.Analyze(@$"
let {varName} = [a, b, c]
");
        var ast = ScriptParser.Parse(tokens)!;

        var let = ast.Commands.Cast<AsLet>().First();
        Assert.Equal(varName, let.Name.Name);
        var arg = Assert.IsType<AsArray>(let.Arg);
        Assert.Contains(arg.Items, item => item.IsIdentity().Name == "a");
        Assert.Contains(arg.Items, item => item.IsIdentity().Name == "b");
        Assert.Contains(arg.Items, item => item.IsIdentity().Name == "c");
    }
    
    [Fact]
    public void CanParseArrayInLetParamsAndElseWhere()
    {
        var varName = "a";
        var tokens = LexicalAnalyzer.Analyze(@$"
let {varName} = [a, b, c]
| a [c,1.5,""2""]

");
        var ast = ScriptParser.Parse(tokens)!;

        var let = ast.Commands.Cast<AsLet>().First();
        Assert.Equal(varName, let.Name.Name);

        let.Arg.IsArray(["a", "b", "c"]);


        Assert.Single(Assert.Single(let.Pipes).Arguments).IsArray(["c", "1.5", "\"2\""]);
    }
    
    [Fact]
    public void CanParseArrayWithReferenceInPipe()
    {
        var varName = "a";
        var tokens = LexicalAnalyzer.Analyze(@$"
let {varName} = [a, b, c]
| a [&]

");
        var ast = ScriptParser.Parse(tokens)!;

        var let = ast.Commands.Cast<AsLet>().First();
        Assert.Equal(varName, let.Name.Name);

        let.Arg.IsArray(["a", "b", "c"]);


        Assert.Single(Assert.Single(let.Pipes).Arguments).IsArray(["&"]);
    }

    [Fact]
    public void CanParseEmptyArray()
    {
        var varName = "a";
        var tokens = LexicalAnalyzer.Analyze(@$"
let {varName} = []
| a []

");
        var ast = ScriptParser.Parse(tokens)!;

        var let = ast.Commands.Cast<AsLet>().First();
        Assert.Equal(varName, let.Name.Name);

        let.Arg.IsArray([]);


        Assert.Single(Assert.Single(let.Pipes).Arguments).IsArray([]);
    }

    [Fact]
    public void ThrowIfArrayNotClose()
    {
        Assert.Throws<InvalidGrammarException>(() =>
        {
            var tokens = LexicalAnalyzer.Analyze(@$"
let a = [
");
            _ = ScriptParser.Parse(tokens);
        });
    }

    [Fact]
    public void ThrowIfArrayEmptyButOnlyComma()
    {
        Assert.Throws<InvalidGrammarException>(() =>
        {
            var tokens = LexicalAnalyzer.Analyze(@$"
let a = [,]
");
            _ = ScriptParser.Parse(tokens);
        });
    }

    [Fact]
    public void ThrowIfReferenceInWrongContext()
    {
        Assert.Throws<InvalidGrammarException>(() =>
        {
            var tokens = LexicalAnalyzer.Analyze(@$"
let a = &
");
            _ = ScriptParser.Parse(tokens);
        });
    }

    [Fact]
    public void ThrowIfReferenceInWrongContextInCall()
    {
        Assert.Throws<InvalidGrammarException>(() =>
        {
            var tokens = LexicalAnalyzer.Analyze(
                """

                     call a &

                     """);
            _ = ScriptParser.Parse(tokens);
        });
    }
}