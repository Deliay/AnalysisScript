using System.Diagnostics.CodeAnalysis;
using AnalysisScript.Interpreter;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AnalysisScript.Interpreter.Variables;
using Json.Path;

namespace AnalysisScript.Library;

public static class BasicFunctionV2
{
    private static readonly Dictionary<string, Regex> RegexCache = [];

    private static Regex GetRegex(string regexStr)
    {
        if (!RegexCache.TryGetValue(regexStr, out var regex))
        {
            RegexCache.Add(regexStr, regex = new Regex(regexStr, RegexOptions.Compiled));
        }

        return regex;
    }
    
    [AsMethod(Name = "select")]
    public static LambdaExpression SelectSingle<T>(AsExecutionContext ctx, T @this, string propertyName)
    {
        var paramGetter = ExprTreeHelper.GetConstantValueLambda(@this);
        var property = Expression.Property(paramGetter, propertyName);

        return Expression.Lambda(property);
    }

    [AsMethod(Name = "select")]
    public static LambdaExpression SelectSequence<T>(AsExecutionContext ctx, IEnumerable<T> @this, string propertyName)
    {
        var param = Expression.Parameter(typeof(T));
        var property = Expression.Property(param, propertyName);

        var thisParam = ExprTreeHelper.GetConstantValueLambda(@this);

        var mapperMethod = Expression.Lambda(property, param);
        var selectMethod = ExprTreeHelper.ConstructSelectMethod(typeof(IEnumerable<>), typeof(T), property.Type, forceSync: true);

        var callSelect = Expression.Call(null, selectMethod, thisParam, mapperMethod);

        return Expression.Lambda(callSelect);
    }

    [AsMethod(Name = "select")]
    public static LambdaExpression SelectSequenceAsync<T>(AsExecutionContext ctx, IAsyncEnumerable<T> @this,
        string propertyName)
    {
        var param = Expression.Parameter(typeof(T));
        var property = Expression.Property(param, propertyName);

        var thisParam = ExprTreeHelper.GetConstantValueLambda(@this);

        var mapperMethod = Expression.Lambda(property, param);
        var selectMethod = ExprTreeHelper.ConstructSelectMethod(@this.GetType(), typeof(T), property.Type);

        var callSelect = Expression.Call(null, selectMethod, thisParam, mapperMethod);

        return Expression.Lambda(callSelect);
    }

    [AsMethod(Name = "join")]
    public static string Join<T>(AsExecutionContext ctx, IEnumerable<T> values, string delimiter)
    {
        return string.Join(delimiter, values);
    }

    [AsMethod(Name = "join")]
    public static async ValueTask<string> Join<T>(AsExecutionContext ctx, IAsyncEnumerable<T> values, string delimiter)
    {
        return string.Join(delimiter, await values.ToListAsync());
    }

    [AsMethod(Name = "filter_contains")]
    public static IEnumerable<string> FilterContainsString(AsExecutionContext ctx, IEnumerable<string> values,
        string contains)
    {
        return values.Where((item) => item.Contains(contains));
    }

    [AsMethod(Name = "filter_contains")]
    public static IAsyncEnumerable<string> FilterContainsStringAsync(AsExecutionContext ctx,
        IAsyncEnumerable<string> values, string contains)
    {
        return values.Where((item) => item.Contains(contains));
    }

    [AsMethod(Name = "filter_contains")]
    public static IEnumerable<T> FilterContains<T>(AsExecutionContext ctx, IEnumerable<T> values, string propertyName,
        string contains)
    {
        var param = Expression.Parameter(typeof(T), "item");
        var propertyGetter = Expression.Property(param, propertyName);

        var valueGetter = ExprTreeHelper.GetExprToStringDelegate<T>(param, propertyGetter);

        return values.Where((item) => valueGetter(item).Contains(contains));
    }

    [AsMethod(Name = "filter_contains")]
    public static IAsyncEnumerable<T> FilterContainsAsync<T>(AsExecutionContext ctx, IAsyncEnumerable<T> values,
        string propertyName, string contains)
    {
        var param = Expression.Parameter(typeof(T), "item");
        var propertyGetter = Expression.Property(param, propertyName);

        var valueGetter = ExprTreeHelper.GetExprToStringDelegate<T>(param, propertyGetter);

        return values.Where((item) => valueGetter(item).Contains(contains));
    }


    [AsMethod(Name = "filter_regex")]
    public static IEnumerable<T> FilterContainsRegex<T>(AsExecutionContext ctx, IEnumerable<T> values,
        string propertyName, string regexStr)
    {
        var param = Expression.Parameter(typeof(T), "item");
        var propertyGetter = Expression.Property(param, propertyName);

        var valueGetter = ExprTreeHelper.GetExprToStringDelegate<T>(param, propertyGetter);

        var regex = GetRegex(regexStr);

        return values.Where((item) => regex.IsMatch(valueGetter(item)));
    }

    [AsMethod(Name = "filter_regex")]
    public static IAsyncEnumerable<T> FilterContainsRegexAsync<T>(AsExecutionContext ctx, IAsyncEnumerable<T> values,
        string propertyName, string regexStr)
    {
        var param = Expression.Parameter(typeof(T), "item");
        var propertyGetter = Expression.Property(param, propertyName);

        var valueGetter = ExprTreeHelper.GetExprToStringDelegate<T>(param, propertyGetter);

        var regex = GetRegex(regexStr);

        return values.Where((item) => regex.IsMatch(valueGetter(item)));
    }

    [AsMethod(Name = "filter_regex")]
    public static IEnumerable<string> FilterContainsStringRegex(AsExecutionContext ctx, IEnumerable<string> values,
        string regexStr)
    {
        var regex = GetRegex(regexStr);

        return values.Where((item) => regex.IsMatch(item));
    }

    [AsMethod(Name = "filter_regex")]
    public static IAsyncEnumerable<string> FilterContainsStringRegexAsync(AsExecutionContext ctx,
        IAsyncEnumerable<string> values, string regexStr)
    {
        var regex = GetRegex(regexStr);

        return values.Where((item) => regex.IsMatch(item));
    }

    [AsMethod(Name = "filter_not_contains")]
    public static IEnumerable<T> FilterNotContains<T>(AsExecutionContext ctx, IEnumerable<T> values,
        string propertyName, string contains)
    {
        var param = Expression.Parameter(typeof(T), "item");
        var propertyGetter = Expression.Property(param, propertyName);

        var valueGetter = ExprTreeHelper.GetExprToStringDelegate<T>(param, propertyGetter);

        return values.Where((item) => !valueGetter(item).Contains(contains));
    }

    [AsMethod(Name = "filter_not_contains")]
    public static IAsyncEnumerable<T> FilterNotContainsAsync<T>(AsExecutionContext ctx, IAsyncEnumerable<T> values,
        string propertyName, string contains)
    {
        var param = Expression.Parameter(typeof(T), "item");
        var propertyGetter = Expression.Property(param, propertyName);

        var valueGetter = ExprTreeHelper.GetExprToStringDelegate<T>(param, propertyGetter);

        return values.Where((item) => !valueGetter(item).Contains(contains));
    }

    [AsMethod(Name = "filter_not_regex")]
    public static IEnumerable<T> FilterNotContainsRegex<T>(AsExecutionContext ctx, IEnumerable<T> values,
        string propertyName, string regexStr)
    {
        var param = Expression.Parameter(typeof(T), "item");
        var propertyGetter = Expression.Property(param, propertyName);

        var valueGetter = ExprTreeHelper.GetExprToStringDelegate<T>(param, propertyGetter);

        var regex = GetRegex(regexStr);

        return values.Where((item) => !regex.IsMatch(valueGetter(item)));
    }

    [AsMethod(Name = "filter_not_regex")]
    public static IAsyncEnumerable<T> FilterNotContainsRegexAsync<T>(AsExecutionContext ctx, IAsyncEnumerable<T> values,
        string propertyName, string regexStr)
    {
        var param = Expression.Parameter(typeof(T), "item");
        var propertyGetter = Expression.Property(param, propertyName);

        var valueGetter = ExprTreeHelper.GetExprToStringDelegate<T>(param, propertyGetter);

        var regex = GetRegex(regexStr);

        return values.Where((item) => !regex.IsMatch(valueGetter(item)));
    }

    [AsMethod(Name = "filter_not_regex")]
    public static IEnumerable<string> FilterNotContainsStringRegex(AsExecutionContext ctx, IEnumerable<string> values,
        string regexStr)
    {
        var regex = GetRegex(regexStr);

        return values.Where((item) => !regex.IsMatch(item));
    }

    [AsMethod(Name = "filter_not_regex")]
    public static IAsyncEnumerable<string> FilterNotContainsStringRegexAsync(AsExecutionContext ctx,
        IAsyncEnumerable<string> values, string regexStr)
    {
        var regex = GetRegex(regexStr);

        return values.Where((item) => !regex.IsMatch(item));
    }

    [AsMethod(Name = "filter_not_contains")]
    public static IEnumerable<string> FilterNotContainsString(AsExecutionContext ctx, IEnumerable<string> values,
        string contains)
    {
        return values.Where((item) => !item.Contains(contains));
    }

    [AsMethod(Name = "filter_contains")]
    public static IAsyncEnumerable<string> FilterNotContainsStringAsync(AsExecutionContext ctx,
        IAsyncEnumerable<string> values, string contains)
    {
        return values.Where((item) => !item.Contains(contains));
    }
    
    [AsMethod(Name = "filter_not_null")]
    public static IEnumerable<T> FilterNotNull<T>(AsExecutionContext ctx, IEnumerable<T> values)
    {
        return values.Where((item) => item is not null);
    }
    
    [AsMethod(Name = "filter_not_null")]
    public static IAsyncEnumerable<T> FilterNotNull<T>(AsExecutionContext ctx, IAsyncEnumerable<T> values)
    {
        return values.Where((item) => item is not null);
    }

    [AsMethod(Name = "json")]
    public static string ToJson<T>(AsExecutionContext ctx, T obj) => JsonSerializer.Serialize(obj);

    [AsMethod(Name = "json")]
    public static async ValueTask<string> ToJson<T>(AsExecutionContext ctx, IAsyncEnumerable<T> obj) =>
        JsonSerializer.Serialize(await obj.ToArrayAsync());

    [AsMethod(Name = "group")]
    [AsMethod(Name = "distinct")]
    public static HashSet<T> Group<T>(AsExecutionContext ctx, IEnumerable<T> values)
        => values.ToHashSet();

    [AsMethod(Name = "group")]
    [AsMethod(Name = "distinct")]
    public static async ValueTask<HashSet<T>> Group<T>(AsExecutionContext ctx, IAsyncEnumerable<T> values)
        => await values.ToHashSetAsync();

    [AsMethod(Name = "limit")]
    [AsMethod(Name = "take")]
    public static IEnumerable<T> Take<T>(AsExecutionContext ctx, IEnumerable<T> source, int count) =>
        source.Take(count);

    [AsMethod(Name = "limit")]
    [AsMethod(Name = "take")]
    public static IAsyncEnumerable<T> Take<T>(AsExecutionContext ctx, IAsyncEnumerable<T> source, int count) =>
        source.Take(count);

    [AsMethod(Name = "skip")]
    public static IEnumerable<T> Skip<T>(AsExecutionContext ctx, IEnumerable<T> source, int count) =>
        source.Skip(count);


    [AsMethod(Name = "split")]
    public static string[] Split(AsExecutionContext ctx, string source, string splitter) => source.Split(splitter);


    [AsMethod(Name = "skip")]
    public static IAsyncEnumerable<T> Skip<T>(AsExecutionContext ctx, IAsyncEnumerable<T> source, int count) =>
        source.Skip(count);


    [AsMethod(Name = "last")]
    public static IEnumerable<T> Last<T>(AsExecutionContext ctx, IEnumerable<T> source, int count) =>
        source.TakeLast(count);

    [AsMethod(Name = "last")]
    public static IAsyncEnumerable<T> Last<T>(AsExecutionContext ctx, IAsyncEnumerable<T> source, int count) =>
        source.TakeLast(count);

    [AsMethod(Name = "not_null")]
    public static T ThrowIfNull<T>(AsExecutionContext ctx, T instance, string msg) =>
        instance ?? throw new NullReferenceException(msg);

    [AsMethod(Name = "not_empty")]
    public static IEnumerable<T> ThrowIfEmpty<T>(AsExecutionContext ctx, IEnumerable<T> instance, string msg) =>
        instance.Any() ? instance : throw new NullReferenceException(msg);

    [AsMethod(Name = "not_empty")]
    public static async ValueTask<IAsyncEnumerable<T>> ThrowIfEmpty<T>(AsExecutionContext ctx,
        IAsyncEnumerable<T> instance, string msg) => await instance.AnyAsync(ctx.CancelToken)
        ? instance : throw new NullReferenceException(msg);

    [AsMethod(Name = "not_empty")]
    public static string ThrowIfEmptyString(AsExecutionContext ctx, string instance, string msg) =>
        instance.Length != 0 ? instance : throw new NullReferenceException(msg);
    
    [AsMethod(Name = "not_null")]
    public static T ThrowIfNull<T>(AsExecutionContext ctx, T instance) =>
        ThrowIfNull(ctx, instance, $"{ctx.CurrentExecuteObject?.LexicalToken.Line.ToString() ?? typeof(T).Name} can not be null");

    [AsMethod(Name = "not_empty")]
    public static IEnumerable<T> ThrowIfEmpty<T>(AsExecutionContext ctx, IEnumerable<T> instance) =>
        ThrowIfEmpty(ctx, instance, $"{ctx.CurrentExecuteObject?.LexicalToken.Line.ToString() ?? typeof(T).Name} can not be empty");

    [AsMethod(Name = "not_empty")]
    public static ValueTask<IAsyncEnumerable<T>> ThrowIfEmpty<T>(AsExecutionContext ctx,
        IAsyncEnumerable<T> instance) => ThrowIfEmpty(ctx, instance,
        $"{ctx.CurrentExecuteObject?.LexicalToken.Line.ToString() ?? typeof(T).Name} can not be empty");

    [AsMethod(Name = "not_empty")]
    public static string ThrowIfEmptyString(AsExecutionContext ctx, string instance) =>
        ThrowIfEmptyString(ctx, instance,
            $"{ctx.CurrentExecuteObject?.LexicalToken.Line.ToString() ?? "string"} can not be empty");

    [AsMethod(Name = "format")]
    public static string Format(AsExecutionContext ctx, string format)
    {
        return ctx.VariableContext.Interpolation(format, ctx.CurrentExecuteObject?.LexicalToken!);
    }

    [AsMethod(Name = "flat")]
    public static IEnumerable<T> Flat<T>(AsExecutionContext ctx, IEnumerable<IEnumerable<T>> arrayList)
    {
        return arrayList.SelectMany(array => array);
    }

    [AsMethod(Name = "flat")]
    public static IAsyncEnumerable<T> FlatAsync<T>(AsExecutionContext ctx, IEnumerable<IAsyncEnumerable<T>> arrayList)
    {
        return arrayList.ToAsyncEnumerable().SelectMany(array => array);
    }

    [AsMethod(Name = "flat")]
    public static IAsyncEnumerable<T> FlatAsync<T>(AsExecutionContext ctx, IAsyncEnumerable<IAsyncEnumerable<T>> arrayList)
    {
        return arrayList.SelectMany(array => array);
    }

    [AsMethod(Name = "flat")]
    public static IAsyncEnumerable<T> FlatAsync<T>(AsExecutionContext ctx, IAsyncEnumerable<IEnumerable<T>> arrayList)
    {
        return arrayList.SelectMany(array => array.ToAsyncEnumerable());
    }

    [AsMethod(Name = "list")]
    public static List<T> ListOf<T>(AsExecutionContext ctx, IEnumerable<T> seq) => seq.ToList();
    
    
    [AsMethod(Name = "list")]
    public static ValueTask<List<T>> ListOfAsync<T>(AsExecutionContext ctx, IAsyncEnumerable<T> seq) => seq.ToListAsync();
    
    
    [AsMethod(Name = "hashset")]
    public static HashSet<T> SetOf<T>(AsExecutionContext ctx, IEnumerable<T> seq) => seq.ToHashSet();
    
    
    [AsMethod(Name = "hashset")]
    public static ValueTask<HashSet<T>> SetOfAsync<T>(AsExecutionContext ctx, IAsyncEnumerable<T> seq) => seq.ToHashSetAsync();

    [AsMethod(Name = "add")]
    public static List<T> Add<T>(AsExecutionContext ctx, List<T> seq, T item)
    {
        seq.Add(item);
        return seq;
    }
    
    [AsMethod(Name = "add")]
    public static HashSet<T> Add<T>(AsExecutionContext ctx, HashSet<T> seq, T item)
    {
        seq.Add(item);
        return seq;
    }
    
    [AsMethod(Name = "add")]
    public static IEnumerable<T> Add<T>(AsExecutionContext ctx, IEnumerable<T> seq, T item)
    {
        return seq.Append(item);
    }
    
    [AsMethod(Name = "add")]
    public static IAsyncEnumerable<T> Add<T>(AsExecutionContext ctx, IAsyncEnumerable<T> seq, T item)
    {
        return seq.Append(item);
    }


    [AsMethod(Name = "json_node")]
    public static JsonNode? ToJsonNode(AsExecutionContext ctx, string obj)
    {
        return JsonNode.Parse(obj);
    }
    
    [AsMethod(Name = "json_node")]
    public static JsonNode? ToJsonNode<T>(AsExecutionContext ctx, T obj)
    {
        return JsonSerializer.SerializeToNode(obj);
    }
    
    [AsMethod(Name = "json_node")]
    public static async ValueTask<JsonNode?> ToJsonNode<T>(AsExecutionContext ctx, IAsyncEnumerable<T> obj)
    {
        return JsonSerializer.SerializeToNode(await obj.ToListAsync(ctx.CancelToken));
    }

    private static readonly Dictionary<string, JsonPath> JsonPathCaches = [];

    private static JsonPath GetJsonPath(string path)
    {
        if (!JsonPathCaches.TryGetValue(path, out var jsonPath)) 
            JsonPathCaches.Add(path, jsonPath = JsonPath.Parse(path));

        return jsonPath;
    }

    [AsMethod(Name = "json_path")]
    public static IEnumerable<JsonNode?> EvalJsonPath(AsExecutionContext ctx, JsonNode? json, string path)
    {
        var jsonPath = GetJsonPath(path);

        var result = jsonPath.Evaluate(json);

        if (result.Error is not null || result.Matches is null || result.Matches.Count == 0)
        {
            return Enumerable.Empty<JsonNode?>();
        }

        return result.Matches.Select(node => node.Value);
    }

    [AsMethod(Name = "json_path")]
    public static IEnumerable<JsonNode?> EvalJsonPath(AsExecutionContext ctx, string obj, string path)
    {
        
        return EvalJsonPath(ctx, ToJsonNode(ctx, obj), path);
    }

    [AsMethod(Name = "json_path")]
    public static IEnumerable<JsonNode?> EvalJsonPath<T>(AsExecutionContext ctx, T obj, string path)
    {
        
        return EvalJsonPath(ctx, ToJsonNode(ctx, obj), path);
    }

    [AsMethod(Name = "json_path")]
    public static async ValueTask<IEnumerable<JsonNode?>> EvalJsonPath<T>(AsExecutionContext ctx, IAsyncEnumerable<T> obj, string path)
    {
        var list = await obj.ToListAsync(ctx.CancelToken);
        return EvalJsonPath(ctx, ToJsonNode(ctx, list), path);
    }

    [AsMethod(Name = "days")]
    public static TimeSpan Days(AsExecutionContext ctx, long duration) => TimeSpan.FromDays(duration);
    
    [AsMethod(Name = "hours")]
    public static TimeSpan Hours(AsExecutionContext ctx, long duration) => TimeSpan.FromHours(duration);
    
    [AsMethod(Name = "minutes")]
    public static TimeSpan Minutes(AsExecutionContext ctx, long duration) => TimeSpan.FromMinutes(duration);
    
    [AsMethod(Name = "seconds")]
    public static TimeSpan Seconds(AsExecutionContext ctx, long duration) => TimeSpan.FromSeconds(duration);
    
    [AsMethod(Name = "add")]
    public static DateTime Add(AsExecutionContext ctx, DateTime time, TimeSpan timeSpan) => time.Add(timeSpan);
    
    [AsMethod(Name = "add")]
    public static DateTimeOffset Add(AsExecutionContext ctx, DateTimeOffset time, TimeSpan timeSpan) => time.Add(timeSpan);

    [AsMethod(Name = "add")]
    public static DateTimeOffset Add(AsExecutionContext ctx, TimeSpan timeSpan, DateTimeOffset time) => time.Add(timeSpan);

    [AsMethod(Name = "eval")]
    public static List<T> Eval<T>(AsExecutionContext ctx, IEnumerable<T> seq) => seq.ToList();

    [AsMethod(Name = "eval")]
    public static ValueTask<List<T>> Eval<T>(AsExecutionContext ctx, IAsyncEnumerable<T> seq) =>
        seq.ToListAsync(ctx.CancelToken);

    [AsMethod(Name = "regex_pluck")]
    public static string? Pluck(AsExecutionContext ctx, string content, string regexStr, int index)
    {
        var regex = GetRegex(regexStr);
        var match = regex.Match(content);
        
        if (!match.Success) return null;

        return match.Groups[index].Success
            ? match.Groups[index].Value
            : null;
    }
    
    [AsMethod(Name = "regex_pluck")]
    public static IEnumerable<string> Pluck(AsExecutionContext ctx, IEnumerable<string> contents, string regexStr, int index)
    {
        var regex = GetRegex(regexStr);

        foreach (var content in contents)
        {
            var match = regex.Match(content);

            if (match.Success && match.Groups[index].Success)
                yield return match.Groups[index].Value;
        }
    }
    
    [AsMethod(Name = "regex_pluck")]
    public static async IAsyncEnumerable<string> PluckAsync(AsExecutionContext ctx, IAsyncEnumerable<string> contents, string regexStr, int index)
    {
        var regex = GetRegex(regexStr);

        await foreach (var content in contents)
        {
            var match = regex.Match(content);

            if (match.Success && match.Groups[index].Success)
                yield return match.Groups[index].Value;
        }
    }

    [AsMethod(Name = "regex_format")]
    public static string? RegexFormat(AsExecutionContext ctx, string content, string regexStr, string format)
    {
        var regex = GetRegex(regexStr);
        var replaceResult = regex.Replace(content, format);
        return ReferenceEquals(replaceResult, content) ? null : replaceResult;
    }

    [AsMethod(Name = "regex_format")]
    public static IEnumerable<string> RegexFormat(AsExecutionContext ctx, IEnumerable<string> content, 
        string regexStr, string format)
    {
        return content
            .Select(str => RegexFormat(ctx, str, regexStr, format))
            .OfType<string>();
    }
    
    [AsMethod(Name = "regex_format")]
    public static IAsyncEnumerable<string> RegexFormatAsync(AsExecutionContext ctx, IAsyncEnumerable<string> content, 
        string regexStr, string format)
    {
        return content
            .Select(str => RegexFormat(ctx, str, regexStr, format))
            .OfType<string>();
    }

    
    public static AsInterpreter RegisterBasicFunctionsV2(this AsInterpreter interpreter)
    {
        interpreter.Variables.Methods.ScanAndRegisterStaticFunction(typeof(BasicFunctionV2));
        return interpreter;
    }
}