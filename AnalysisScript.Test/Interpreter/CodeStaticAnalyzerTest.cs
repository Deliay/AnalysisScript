using AnalysisScript.Interpreter;
using AnalysisScript.Interpreter.Variables;
using AnalysisScript.Interpreter.Variables.Method;
using AnalysisScript.Lexical;
using AnalysisScript.Library;
using AnalysisScript.Parser;
using AnalysisScript.Parser.Ast.Basic;

namespace AnalysisScript.Test.Interpreter;

public class CodeStaticAnalyzerTest
{
    private static VariableContext GenerateContext() =>
        new(new MethodContext()
            .ScanAndRegisterStaticFunction(typeof(BasicFunctionV2)));

    private readonly VariableContext _defaultContext = GenerateContext();
    
    [Fact]
    public void TestCanPreviewLexicalError()
    {
        const string source =
            """
            let b = 1
            let a = +2
            """;

        var dryRunner = new CodeStaticAnalyzer(GenerateContext());

        var result = dryRunner.PreviewErrors(source);
        
        AssertAnalyze<InvalidTokenException>(2, CodeStaticAnalyzer.ErrorTypes.Lexical)
            (Assert.Single(result));
    }
    
    [Fact]
    public void TestCanPreviewGrammarError()
    {
        const string source =
            """
            let b = 1
            leta = 2
            """;

        var dryRunner = new CodeStaticAnalyzer(GenerateContext());

        var result = dryRunner.PreviewErrors(source);
        
        AssertAnalyze<InvalidGrammarException>(2, CodeStaticAnalyzer.ErrorTypes.Parser)
            (Assert.Single(result));
    }

    [Fact]
    public void TestCanPassEmptyAst()
    {
        Assert.Empty(new CodeStaticAnalyzer(_defaultContext).PreviewErrors("#"));
    }

    [Fact]
    public void TestRaiseErrorWhenParamNotInitialize()
    {
        const string source =
            """
            param a
            param b
            """;
        var context = GenerateContext()
            .AddInitializeVariable("a", 1);
        var analyzer = new CodeStaticAnalyzer(context);

        var result = analyzer.PreviewErrors(source);

        var runtimeException = AssertAnalyze<AsRuntimeException>(2, CodeStaticAnalyzer.ErrorTypes.Runtime)
            (Assert.Single(result));
        
        Assert.Equal(AsRuntimeError.VariableNotInitialized, runtimeException.RuntimeError);
    }

    [Fact]
    public void TestNoErrorIfReturnExistVariable()
    {
        const string source =
            """
            param a
            return a
            """;
        var context = GenerateContext()
            .AddInitializeVariable("a", 1);
        var analyzer = new CodeStaticAnalyzer(context);
        var previewErrors = analyzer.PreviewErrors<int>(source);
        
        Assert.Empty(previewErrors);

        Assert.Equal(typeof(int), Assert.Single(analyzer.VariableTypes).Value);
        Assert.Equal(typeof(int), analyzer.ReturnType);
    }

    private static Func<(int, CodeStaticAnalyzer.ErrorTypes, Exception), T> AssertAnalyze<T>(
        int line, CodeStaticAnalyzer.ErrorTypes errorType)
        where T : Exception
    {
        return tuple =>
        {
            Assert.Equal(line, tuple.Item1);
            Assert.Equal(errorType, tuple.Item2);

            return Assert.IsType<T>(tuple.Item3);
        };
    }
    
    [Fact]
    public void TestRaiseErrorIfReturnVariableNotFound()
    {
        const string source =
            """
            param a
            return b
            """;
        var context = GenerateContext()
            .AddInitializeVariable("a", 1);
        var analyzer = new CodeStaticAnalyzer(context);
        var result = analyzer.PreviewErrors<long>(source);


        var runtimeException = AssertAnalyze<AsRuntimeException>(2, CodeStaticAnalyzer.ErrorTypes.Runtime)
            (Assert.Single(result));
        
        Assert.Equal(AsRuntimeError.VariableNotInitialized, runtimeException.RuntimeError);
    }
    
    [Fact]
    public void TestRaiseErrorIfReturnVariableMismatch()
    {
        const string source =
            """
            param a
            return a
            """;
        var context = GenerateContext()
            .AddInitializeVariable("a", 1);
        var analyzer = new CodeStaticAnalyzer(context);
        var result = analyzer.PreviewErrors<long>(source);


        var runtimeException = AssertAnalyze<AsRuntimeException>(2, CodeStaticAnalyzer.ErrorTypes.Runtime)
            (Assert.Single(result));
        
        Assert.Equal(AsRuntimeError.InvalidReturnType, runtimeException.RuntimeError);
    }

    [Fact]
    public void TestNoErrorIfCallMethodCorrectly()
    {
        const string source =
            """
            call days 1
            """;
        var analyzer = new CodeStaticAnalyzer(GenerateContext());
        var result = analyzer.PreviewErrors(source);
        
        Assert.Empty(result);
    }
    
    [Fact]
    public void TestRaiseErrorWhenMethodArgumentMismatch()
    {
        const string source =
            """
            call days 1 2
            """;
        var analyzer = new CodeStaticAnalyzer(GenerateContext());
        var result = analyzer.PreviewErrors(source);

        var runtimeException = AssertAnalyze<AsRuntimeException>(1, CodeStaticAnalyzer.ErrorTypes.Runtime)
            (Assert.Single(result));
        
        Assert.Equal(AsRuntimeError.NoMatchedMethod, runtimeException.RuntimeError);
    }
    
    [Fact]
    public void TestRaiseErrorWhenMethodNotFound()
    {
        const string source =
            """
            call b 1 2
            """;
        var analyzer = new CodeStaticAnalyzer(GenerateContext());
        var result = analyzer.PreviewErrors(source);

        var runtimeException = AssertAnalyze<AsRuntimeException>(1, CodeStaticAnalyzer.ErrorTypes.Runtime)
            (Assert.Single(result));
        
        Assert.Equal(AsRuntimeError.NoMatchedMethod, runtimeException.RuntimeError);
    }
    
    [Fact]
    public void TestLetWithNoPipes()
    {
        const string source =
            """
            
            let a = ""
            
            """;
        var analyzer = new CodeStaticAnalyzer(GenerateContext());
        var result = analyzer.PreviewErrors(source);

        Assert.Empty(result);
        Assert.Equal(typeof(string), Assert.Single(analyzer.VariableTypes).Value);
    }
    [Fact]
    public void TestLetWithNoPipesWillRaiseErrorIfVariableDuplicated()
    {
        const string source =
            """

            let a = ""
            let a = ""

            """;
        var analyzer = new CodeStaticAnalyzer(GenerateContext());
        var result = analyzer.PreviewErrors(source);

        var runtimeException = AssertAnalyze<AsRuntimeException>(3, CodeStaticAnalyzer.ErrorTypes.Runtime)
            (Assert.Single(result));
        
        Assert.Equal(AsRuntimeError.VariableAlreadyExist, runtimeException.RuntimeError);
    }
    [Fact]
    public void TestLetWithNoPipesWillRaiseErrorIfMethodNotFound()
    {
        const string source =
            """

            let a = ""
            | b

            """;
        var analyzer = new CodeStaticAnalyzer(GenerateContext());
        var result = analyzer.PreviewErrors(source);

        var runtimeException = AssertAnalyze<AsRuntimeException>(3, CodeStaticAnalyzer.ErrorTypes.Runtime)
            (Assert.Single(result));
        
        Assert.Equal(AsRuntimeError.NoMatchedMethod, runtimeException.RuntimeError);
    }
    [Fact]
    public void TestLetWithNoPipesWillRaiseErrorIfMethodArgumentMisMatch()
    {
        const string source =
            """

            let a = ""
            | join [1]

            """;
        var analyzer = new CodeStaticAnalyzer(GenerateContext());
        var result = analyzer.PreviewErrors(source);

        var runtimeException = AssertAnalyze<AsRuntimeException>(3, CodeStaticAnalyzer.ErrorTypes.Runtime)
            (Assert.Single(result));
        
        Assert.Equal(AsRuntimeError.NoMatchedMethod, runtimeException.RuntimeError);
    }
    [Fact]
    public void TestLetPipesWithoutErrors()
    {
        const string source =
            """
            let a = [1, 2]
            | join ","
            | split ","
            | distinct
            | take 1

            """;
        var context = GenerateContext();
        var analyzer = new CodeStaticAnalyzer(context);
        var result = analyzer.PreviewErrors(source);

        Assert.Empty(result);

        var aType = analyzer.VariableTypes[new AsIdentity(new Token.Identity("a", 0, 0))];
        Assert.Equal(typeof(IEnumerable<string>), aType);
        var (_, line2Type) = Assert.Single(analyzer.VariableTypes.Where(p => p.Key.LexicalToken.Line == 2));
        Assert.Equal(typeof(string), line2Type);
        var (_, line3Type) = Assert.Single(analyzer.VariableTypes.Where(p => p.Key.LexicalToken.Line == 3));
        Assert.Equal(typeof(string).MakeArrayType(), line3Type);
    }
    [Fact]
    public void TestLetPipesForEachWithoutErrors()
    {
        const string source =
            """
            let a = ["1,2", "3,4"]
            ||* split & ","
            | flat
            | distinct
            | take 1
            | join ","

            """;
        var context = GenerateContext();
        var analyzer = new CodeStaticAnalyzer(context);
        var result = analyzer.PreviewErrors(source);

        Assert.Empty(result);

        var aType = analyzer.VariableTypes[new AsIdentity(new Token.Identity("a", 0, 0))];
        Assert.Equal(typeof(string), aType);
        var (_, line2Type) = Assert.Single(analyzer.VariableTypes.Where(p => p.Key.LexicalToken.Line == 2));
        Assert.Equal(typeof(IAsyncEnumerable<string[]>), line2Type);
        var (_, line3Type) = Assert.Single(analyzer.VariableTypes.Where(p => p.Key.LexicalToken.Line == 3));
        Assert.Equal(typeof(IAsyncEnumerable<string>), line3Type);
        var (_, line4Type) = Assert.Single(analyzer.VariableTypes.Where(p => p.Key.LexicalToken.Line == 4));
        Assert.Equal(typeof(IAsyncEnumerable<string>), line4Type);
        var (_, line5Type) = Assert.Single(analyzer.VariableTypes.Where(p => p.Key.LexicalToken.Line == 5));
        Assert.Equal(typeof(IAsyncEnumerable<string>), line5Type);
        var (_, line6Type) = Assert.Single(analyzer.VariableTypes.Where(p => p.Key.LexicalToken.Line == 6));
        Assert.Equal(aType, line6Type);
    }
}