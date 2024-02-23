using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using AnalysisScript.Interpreter;
using AnalysisScript.Interpreter.Variables;
using AnalysisScript.Library;

namespace AnalysisScript.Test;

public class LibraryTest
{
    private static readonly AsExecutionContext DefaultContext = new();
    
    [Fact]
    public void TestSelectSingle()
    {
        List<int> test = [1,2];
        var prop = nameof(test.Count);
        var lambda = BasicFunctionV2.SelectSingle(DefaultContext, test, prop);

        var method = Expression.Lambda<Func<int>>(Expression.Invoke(lambda)).Compile();
        
        Assert.Equal(2, method());
    }
    
    [Fact]
    public async Task TestSelectEnumerable()
    {
        List<List<int>> test = [[1],[2]];
        var prop = nameof(test.Count);
        var lambda = BasicFunctionV2.SelectSequence(DefaultContext, test, prop);

        var method = Expression.Lambda<Func<IAsyncEnumerable<int>>>(Expression.Invoke(lambda)).Compile();

        var result = method();
        Assert.Equal(2, await result.SumAsync());
    }
    [Fact]
    public async Task TestSelectAsyncEnumerable()
    {
        List<List<int>> test = [[1],[2]];
        var prop = nameof(test.Count);
        var lambda = BasicFunctionV2.SelectSequenceAsync(DefaultContext, test.ToAsyncEnumerable(), prop);

        var method = Expression.Lambda<Func<IAsyncEnumerable<int>>>(Expression.Invoke(lambda)).Compile();

        var result = method();
        Assert.Equal(2, await result.SumAsync());
    }

    [Fact]
    public void TestJoin()
    {
        List<int> seq = [1, 2];
        const string delimiter = ",";
        var result = BasicFunctionV2.Join(DefaultContext, seq, delimiter);
        
        Assert.Equal(string.Join(delimiter, seq), result);
    }

    [Fact]
    public async Task TestJoinAsync()
    {
        List<int> seq = [1, 2];
        const string delimiter = ",";
        var asyncSeq = seq.ToAsyncEnumerable();

        var result = await BasicFunctionV2.Join(DefaultContext, asyncSeq, delimiter);

        var expect = string.Join(delimiter, await asyncSeq.ToListAsync());
        
        Assert.Equal(expect, result);
    }

    [Fact]
    public void TestFilterContainsString()
    {
        var result = BasicFunctionV2.FilterContainsString(DefaultContext, ["ab", "ac", "d"], "d");
        
        Assert.Equal("d", Assert.Single(result));
    }

    [Fact]
    public async Task TestFilterContainStringAsync()
    {
        var arr = (new List<string> { "ab", "ac", "d" }).ToAsyncEnumerable();

        var filtered = BasicFunctionV2.FilterContainsStringAsync(DefaultContext, arr, "d");

        Assert.Equal("d", Assert.Single(await filtered.ToListAsync()));
        
    }

    [Fact]
    public void TestFilterContainsProperty()
    {
        var items = new List<Type>() { typeof(int), typeof(string) };

        var result = BasicFunctionV2.FilterContains(DefaultContext, items, "Name", "Int");
        
        Assert.Equal(typeof(int), Assert.Single(result));
    }

    [Fact]
    public async Task TestFilterContainsPropertyAsync()
    {
        var items = new List<Type>() { typeof(int), typeof(string) }.ToAsyncEnumerable();

        var result = BasicFunctionV2.FilterContainsAsync(DefaultContext, items, "Name", "Int");
        
        Assert.Equal(typeof(int), Assert.Single(await result.ToListAsync()));
    }
    
    
    [Fact]
    public void TestFilterContainsStringRegex()
    {
        var result = BasicFunctionV2.FilterContainsStringRegex(DefaultContext, ["ab", "ac", "ad"], ".d");
        
        Assert.Equal("ad", Assert.Single(result));
    }

    [Fact]
    public async Task TestFilterContainStringRegexAsync()
    {
        var arr = (new List<string> { "ab", "ac", "ad" }).ToAsyncEnumerable();

        var filtered = BasicFunctionV2.FilterContainsStringRegexAsync(DefaultContext, arr, ".d");

        Assert.Equal("ad", Assert.Single(await filtered.ToListAsync()));
        
    }

    [Fact]
    public void TestFilterContainsPropertyRegex()
    {
        var items = new List<Type>() { typeof(int), typeof(string) };

        var result = BasicFunctionV2.FilterContainsRegex(DefaultContext, items, "Name", "I.t");
        
        Assert.Equal(typeof(int), Assert.Single(result));
    }

    [Fact]
    public async Task TestFilterContainsPropertyRegexAsync()
    {
        var items = new List<Type>() { typeof(int), typeof(string) }.ToAsyncEnumerable();

        var result = BasicFunctionV2.FilterContainsRegexAsync(DefaultContext, items, "Name", "I.t");
        
        Assert.Equal(typeof(int), Assert.Single(await result.ToListAsync()));
    }
    
    [Fact]
    public void TestFilterNotContainsString()
    {
        var result = BasicFunctionV2.FilterNotContainsString(DefaultContext, ["ab", "ac", "d"], "a");
        
        Assert.Equal("d", Assert.Single(result));
    }

    [Fact]
    public async Task TestFilterNotContainStringAsync()
    {
        var arr = (new List<string> { "ab", "ac", "d" }).ToAsyncEnumerable();

        var filtered = BasicFunctionV2.FilterNotContainsStringAsync(DefaultContext, arr, "a");

        Assert.Equal("d", Assert.Single(await filtered.ToListAsync()));
        
    }

    [Fact]
    public void TestFilterNotContainsProperty()
    {
        var items = new List<Type>() { typeof(int), typeof(string) };

        var result = BasicFunctionV2.FilterNotContains(DefaultContext, items, "Name", "String");
        
        Assert.Equal(typeof(int), Assert.Single(result));
    }

    [Fact]
    public async Task TestFilterNotContainsPropertyAsync()
    {
        var items = new List<Type>() { typeof(int), typeof(string) }.ToAsyncEnumerable();

        var result = BasicFunctionV2.FilterNotContainsAsync(DefaultContext, items, "Name", "String");
        
        Assert.Equal(typeof(int), Assert.Single(await result.ToListAsync()));
    }
    
    
    [Fact]
    public void TestFilterNotContainsStringRegex()
    {
        var result = BasicFunctionV2.FilterNotContainsStringRegex(DefaultContext, ["ab", "ac", "d"], "a.");
        
        Assert.Equal("d", Assert.Single(result));
    }

    [Fact]
    public async Task TestFilterNotContainStringRegexAsync()
    {
        var arr = (new List<string> { "ab", "ac", "d" }).ToAsyncEnumerable();

        var filtered = BasicFunctionV2.FilterNotContainsStringRegexAsync(DefaultContext, arr, "a.");

        Assert.Equal("d", Assert.Single(await filtered.ToListAsync()));
        
    }

    [Fact]
    public void TestFilterNotContainsPropertyRegex()
    {
        var items = new List<Type>() { typeof(int), typeof(string) };

        var result = BasicFunctionV2.FilterNotContainsRegex(DefaultContext, items, "Name", "^S.*");
        
        Assert.Equal(typeof(int), Assert.Single(result));
    }

    [Fact]
    public async Task TestFilterNotContainsPropertyRegexAsync()
    {
        var items = new List<Type>() { typeof(int), typeof(string) }.ToAsyncEnumerable();

        var result = BasicFunctionV2.FilterNotContainsRegexAsync(DefaultContext, items, "Name", "^S.*");
        
        Assert.Equal(typeof(int), Assert.Single(await result.ToListAsync()));
    }

    [Fact]
    public void TestJson()
    {
        IEnumerable<int> items = [1,2];

        var array = items as int[] ?? items.ToArray();
        
        var actual = BasicFunctionV2.ToJson(DefaultContext, array);
        
        Assert.Equal(JsonSerializer.Serialize(array), actual);
    }
    [Fact]
    public async Task TestJsonAsync()
    {
        IEnumerable<int> items = [1,2];

        var array = items.ToAsyncEnumerable();
        
        var actual = await BasicFunctionV2.ToJson(DefaultContext, array);
        
        Assert.Equal(JsonSerializer.Serialize(items), actual);
    }
    [Fact]
    public void TestGroup()
    {
        IEnumerable<int> items = [1, 2, 1, 2];
        var actual = BasicFunctionV2.Group(DefaultContext, items);
        
        Assert.Equal(2, actual.Count);
        Assert.Equal(3, actual.Sum());
    }
    [Fact]
    public async Task TestGroupAsync()
    {
        IEnumerable<int> items = [1, 2, 1, 2];
        var actual = await BasicFunctionV2.Group(DefaultContext, items.ToAsyncEnumerable());
        
        Assert.Equal(2, actual.Count);
        Assert.Equal(3, actual.Sum());
    }

    [Fact]
    public void TestTake()
    {
        IEnumerable<int> items = [1, 2, 1, 2];
        var actual = BasicFunctionV2.Take(DefaultContext, items, 1);
        
        Assert.Equal(1, Assert.Single(actual));
    }

    [Fact]
    public async Task TestTakeAsync()
    {
        IEnumerable<int> items = [1, 2, 1, 2];
        var actual = BasicFunctionV2.Take(DefaultContext, items.ToAsyncEnumerable(), 1);
        
        Assert.Equal(1, Assert.Single(await actual.ToListAsync()));
    }

    [Fact]
    public void TestSkip()
    {
        IEnumerable<int> items = [1, 2, 1, 2];
        var actual = BasicFunctionV2.Skip(DefaultContext, items, 3);
        
        Assert.Equal(2, Assert.Single(actual));
    }

    [Fact]
    public async Task TestSkipAsync()
    {
        IEnumerable<int> items = [1, 2, 1, 2];
        var actual = BasicFunctionV2.Skip(DefaultContext, items.ToAsyncEnumerable(), 3);
        
        Assert.Equal(2, Assert.Single(await actual.ToListAsync()));
    }

    [Fact]
    public void TestSplit()
    {
        const string test = "a-b-c";
        const string splitter = "-";

        var actual = BasicFunctionV2.Split(DefaultContext, test, splitter);
        
        Assert.Equal(test.Split(splitter), actual);
    }
    

    [Fact]
    public void TestLast()
    {
        IEnumerable<int> items = [1, 2, 1, 2];
        var actual = BasicFunctionV2.Last(DefaultContext, items, 1);
        
        Assert.Equal(2, Assert.Single(actual));
    }

    [Fact]
    public async Task TestLastAsync()
    {
        IEnumerable<int> items = [1, 2, 1, 2];
        var actual = BasicFunctionV2.Last(DefaultContext, items.ToAsyncEnumerable(), 1);
        
        Assert.Equal(2, Assert.Single(await actual.ToListAsync()));
    }

    [Fact]
    public void TestThrowIfNull()
    {
        Assert.Throws<NullReferenceException>(() =>
        {
            BasicFunctionV2.ThrowIfNull<string>(DefaultContext, null!);
        });

        const string expected = "1";
        var actual = BasicFunctionV2.ThrowIfNull<string>(DefaultContext, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestNotEmpty()
    {
        Assert.Throws<NullReferenceException>(() =>
        {
            BasicFunctionV2.ThrowIfEmpty(DefaultContext, Enumerable.Empty<int>());
        });
        Assert.Equal(1, Assert.Single(BasicFunctionV2.ThrowIfEmpty(DefaultContext, [1])));
    }

    [Fact]
    public async Task TestNotEmptyAsync()
    {
        await Assert.ThrowsAsync<NullReferenceException>(async () =>
        {
            await BasicFunctionV2.ThrowIfEmpty(DefaultContext, Enumerable.Empty<int>().ToAsyncEnumerable());
        });
        IEnumerable<int> seq = [1];
        var asyncSeq = seq.ToAsyncEnumerable();

        var actual = await BasicFunctionV2.ThrowIfEmpty(DefaultContext, asyncSeq);

        Assert.Equal(1, Assert.Single(await actual.ToListAsync()));
    }
    
    [Fact]
    public void TestNotEmptyString()
    {
        Assert.Throws<NullReferenceException>(() =>
        {
            BasicFunctionV2.ThrowIfEmptyString(DefaultContext, "");
        });
        Assert.Equal("123", BasicFunctionV2.ThrowIfEmptyString(DefaultContext, "123"));
    }
    
    [Fact]
    public void TestStringInterpolation()
    {
        var variables = new VariableContext();
        variables.AddInitializeVariable("a", "123");

        var ctx = new AsExecutionContext(variables, null!, default);

        const string format = "${a}";
        var actual = BasicFunctionV2.Format(ctx, format);
        
        Assert.Equal(variables.Interpolation(format, null!), actual);
    }

    [Fact]
    public void TestFlat()
    {
        IEnumerable<IEnumerable<int>> seq = [[1, 2], [3, 4]];

        var actual = BasicFunctionV2.Flat(DefaultContext, seq);
        
        Assert.Equal(10, actual.Sum());
    }

    [Fact]
    public async Task TestFlatInnerEnumAsync()
    {
        IEnumerable<IEnumerable<int>> seq = [[1, 2], [3, 4]];

        var asyncSeq = seq.ToAsyncEnumerable();

        var actual = BasicFunctionV2.FlatAsync(DefaultContext, asyncSeq);
        
        Assert.Equal(10, await actual.SumAsync());
    }

    [Fact]
    public async Task TestFlatInnerAsyncEnumAsync()
    {
        IEnumerable<IEnumerable<int>> seq = [[1, 2], [3, 4]];

        var asyncSeq = seq.ToAsyncEnumerable()
            .Select(item => item.ToAsyncEnumerable());

        var actual = BasicFunctionV2.FlatAsync(DefaultContext, asyncSeq);
        
        Assert.Equal(10, await actual.SumAsync());
    }

    [Fact]
    public void TestListOf()
    {
        var actual = BasicFunctionV2.ListOf(DefaultContext, [1]);

        Assert.Equal(1, Assert.Single(Assert.IsType<List<int>>(actual)));
    }

    [Fact]
    public async Task TestListOfAsync()
    {
        IEnumerable<int> seq = [1];
        
        var actual = await BasicFunctionV2.ListOfAsync(DefaultContext, seq.ToAsyncEnumerable());

        Assert.Equal(1, Assert.Single(Assert.IsType<List<int>>(actual)));
    }

    [Fact]
    public void TestSetOf()
    {
        var actual = BasicFunctionV2.SetOf(DefaultContext, [1]);

        Assert.Equal(1, Assert.Single(Assert.IsType<HashSet<int>>(actual)));
    }

    [Fact]
    public async Task TestSetOfAsync()
    {
        IEnumerable<int> seq = [1];
        
        var actual = await BasicFunctionV2.SetOfAsync(DefaultContext, seq.ToAsyncEnumerable());

        Assert.Equal(1, Assert.Single(Assert.IsType<HashSet<int>>(actual)));
    }

    [Fact]
    public void TestAddList()
    {
        var actual = BasicFunctionV2.Add(DefaultContext, new List<int>(), 1);

        Assert.Equal(1, Assert.Single(Assert.IsType<List<int>>(actual)));
    }

    [Fact]
    public void TestAddSet()
    {
        var actual = BasicFunctionV2.Add(DefaultContext, new HashSet<int>(), 1);

        Assert.Equal(1, Assert.Single(Assert.IsType<HashSet<int>>(actual)));
    }

    [Fact]
    public void TestAddEnumerable()
    {
        
        var actual = BasicFunctionV2.Add(DefaultContext, Enumerable.Empty<int>(), 1);

        Assert.Equal(1, Assert.Single(actual));
    }

    [Fact]
    public async Task TestAddAsyncEnumerable()
    {
        
        var actual = BasicFunctionV2.Add(DefaultContext, Enumerable.Empty<int>().ToAsyncEnumerable(), 1);

        Assert.Equal(1, Assert.Single(await actual.ToListAsync()));
    }

    [Fact]
    public void TestFilterIsNotNull()
    {
        var actual = BasicFunctionV2.FilterNotNull(DefaultContext, ["", "", null, ""]);
        
        Assert.Equal(3, actual.Count());
    }

    [Fact]
    public async Task TestFilterIsNotNullAsync()
    {
        IEnumerable<string?> enumerable = ["", "", null, ""];
        var actual = await BasicFunctionV2.FilterNotNull(DefaultContext, enumerable.ToAsyncEnumerable()).ToListAsync();
        
        Assert.Equal(3, actual.Count);
    }

    [Fact]
    public void TestParseStringToJsonNode()
    {
        var jsonNode = BasicFunctionV2.ToJsonNode(DefaultContext, "{ \"a\": 1}");
        
        Assert.NotNull(jsonNode);
        Assert.NotNull(jsonNode["a"]);
        Assert.Equal(1, jsonNode["a"]!.GetValue<int>());
    }

    [Fact]
    public void TestParseStringToJsonNodeByObject()
    {
        var jsonNode = BasicFunctionV2.ToJsonNode(DefaultContext, new { a = 1 });
        
        Assert.NotNull(jsonNode);
        Assert.NotNull(jsonNode["a"]);
        Assert.Equal(1, jsonNode["a"]!.GetValue<int>());
    }

    [Fact]
    public async Task TestParseStringToJsonNodeByAsyncEnumerable()
    {
        var list = Enumerable.Repeat(new { a = 1 }, 1).ToAsyncEnumerable();
        var jsonNode = await BasicFunctionV2.ToJsonNode(DefaultContext, list);
        
        Assert.NotNull(jsonNode);
        Assert.Equal(1, Assert.Single(jsonNode.AsArray())!["a"]!.GetValue<int>());
    }

    [Fact]
    public void TestEvalJsonPathInJsonNode()
    {
        var node = JsonSerializer.SerializeToNode(new { a = 1 });

        var result = BasicFunctionV2.EvalJsonPath(DefaultContext, node, "$.a");
        
        Assert.Equal(1, Assert.Single(result)!.GetValue<int>());
    }

    [Fact]
    public void TestEvalJsonPathInJsonNodeResultEmpty()
    {
        var node = JsonSerializer.SerializeToNode(new { a = 1 });

        var result = BasicFunctionV2.EvalJsonPath(DefaultContext, node, "$.b");
        
        Assert.Empty(result);
    }

    [Fact]
    public void TestEvalJsonPathInJsonString()
    {
        var node = JsonSerializer.Serialize(new { a = 1 });

        var result = BasicFunctionV2.EvalJsonPath(DefaultContext, node, "$.a");
        
        Assert.Equal(1, Assert.Single(result).GetValue<int>());
    }

    [Fact]
    public void TestEvalJsonPathInObject()
    {
        var result = BasicFunctionV2.EvalJsonPath(DefaultContext, new { a = 1 }, "$.a");
        
        Assert.Equal(1, Assert.Single(result).GetValue<int>());
    }

    [Fact]
    public async Task TestEvalJsonPathInAsyncEnumerable()
    {
        var list = Enumerable.Repeat(new { a = 1 }, 1).ToAsyncEnumerable();
        var result = await BasicFunctionV2.EvalJsonPath(DefaultContext, list, "$[0].a");
        
        Assert.Equal(1, Assert.Single(result).GetValue<int>());
    }

    [Fact]
    public void TestTimeSpanGenerator()
    {
        Assert.Equal(TimeSpan.FromSeconds(1), BasicFunctionV2.Seconds(DefaultContext, 1));
        Assert.Equal(TimeSpan.FromMinutes(1), BasicFunctionV2.Minutes(DefaultContext, 1));
        Assert.Equal(TimeSpan.FromHours(1), BasicFunctionV2.Hours(DefaultContext, 1));
        Assert.Equal(TimeSpan.FromDays(1), BasicFunctionV2.Days(DefaultContext, 1));
    }

    [Fact]
    public void TestDateTimeAdd()
    {
        var now = DateTime.Now;
        var span = TimeSpan.FromDays(1);
        Assert.Equal(now + span, BasicFunctionV2.Add(DefaultContext, now, span));
    }

    [Fact]
    public void TestDateTimeOffsetAdd()
    {
        var now = DateTimeOffset.Now;
        var span = TimeSpan.FromDays(1);
        Assert.Equal(now + span, BasicFunctionV2.Add(DefaultContext, now, span));
    }

    [Fact]
    public void TestDateTimeOffsetAdd2()
    {
        var now = DateTimeOffset.Now;
        var span = TimeSpan.FromDays(1);
        Assert.Equal(now + span, BasicFunctionV2.Add(DefaultContext, span, now));
    }

    [Fact]
    public void TestCanEvalSequence()
    {
        IEnumerable<int> seq = [1];
        Assert.Equal(1, Assert.Single(BasicFunctionV2.Eval(DefaultContext, seq)));
    }
    
    [Fact]
    public async Task TestCanEvalSequenceAsync()
    {
        IEnumerable<int> seq = [1];
        var asyncSeq = seq.ToAsyncEnumerable();
        Assert.Equal(1, Assert.Single(await BasicFunctionV2.Eval(DefaultContext, asyncSeq)));
    }

    [Fact]
    public void TestPluckSingleElement()
    {
        var result = BasicFunctionV2.Pluck(DefaultContext, "rlt:1", @"rlt:(\d+)", 1);
        
        Assert.Equal("1", result);
    }

    [Fact]
    public void TestPluckSingleElementReturnNullIfNotMatched()
    {
        var result = BasicFunctionV2.Pluck(DefaultContext, "rlt:a", @"rlt:(\d+)", 1);
        
        Assert.Null(result);
    }

    [Fact]
    public void TestPluckManyElement()
    {
        var result = BasicFunctionV2.Pluck(DefaultContext, ["rlt:aa", "rlt:ab", "rlt:bb"], @"rlt:(a.)", 1);
        
        Assert.Equal("aa,ab", string.Join(',', result));
    }

    [Fact]
    public async Task TestPluckManyElementAsync()
    {
        IEnumerable<string> items = ["rlt:aa", "rlt:ab", "rlt:bb"];
        var result = await BasicFunctionV2
            .PluckAsync(DefaultContext, items.ToAsyncEnumerable(), @"rlt:(a.)", 1)
            .ToListAsync();
        
        Assert.Equal("aa,ab", string.Join(',', result));
    }

    [Fact]
    public void TestRegexFormat()
    {

        var variables = new VariableContext();

        var ctx = new AsExecutionContext(variables, null!, default);

        var actual = BasicFunctionV2.RegexFormat(ctx, "123abc456", @"^(\d+)[a-z]+(\d+)", "$1$2");
        
        Assert.Equal("123456", actual);
    }
    [Fact]
    public void TestRegexFormatReturnNullIfMatchFailed()
    {

        var variables = new VariableContext();

        var ctx = new AsExecutionContext(variables, null!, default);

        var actual = BasicFunctionV2.RegexFormat(ctx, "a123abc456", @"^(\d+)[a-z]+(\d+)", "$1$2");
        
        Assert.Null(actual);
    }

    [Fact]
    public void TestRegexFormatMany()
    {
        var variables = new VariableContext();

        var ctx = new AsExecutionContext(variables, null!, default);

        IEnumerable<string> raw = ["123abc456", "abc123def"];
        
        var actual = BasicFunctionV2.RegexFormat(ctx, raw, @"^(\d+)[a-z]+(\d+)", "$1$2");
        
        Assert.Equal("123456", Assert.Single(actual));
    }
    [Fact]
    public async Task TestRegexFormatManyAsync()
    {
        var variables = new VariableContext();

        var ctx = new AsExecutionContext(variables, null!, default);

        IEnumerable<string> raw = ["123abc456", "abc123def"];
        
        var actual = await BasicFunctionV2.RegexFormatAsync(ctx, raw.ToAsyncEnumerable(), @"^(\d+)[a-z]+(\d+)", "$1$2")
            .ToListAsync();
        
        Assert.Equal("123456", Assert.Single(actual));
    }
}