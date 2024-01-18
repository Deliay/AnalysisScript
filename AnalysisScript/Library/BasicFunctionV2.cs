using AnalysisScript.Interpreter;
using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using AnalysisScript.Interpreter.Variables;
using AnalysisScript.Parser.Ast.Basic;

namespace AnalysisScript.Library;

public static class BasicFunctionV2
{
    private static readonly Dictionary<string, Regex> RegexCache = [];

    private static Regex GetRegex(string regexStr)
    {
        if (!RegexCache.TryGetValue(regexStr, out var regex))
        {
            RegexCache.Add(regexStr, regex = new Regex(regexStr));
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
        var selectMethod = ExprTreeHelper.ConstructSelectMethod(typeof(T), property.Type);

        var callSelect = Expression.Call(null, selectMethod, thisParam, mapperMethod);

        return Expression.Lambda(callSelect);
    }

    [AsMethod(Name = "join")]
    public static string Join<T>(AsExecutionContext ctx, IEnumerable<T> values, string delimiter)
    {
        return string.Join(delimiter, values);
    }

    [AsMethod(Name = "filter_contains")]
    public static IEnumerable<string> FilterContainsString(AsExecutionContext ctx, IEnumerable<string> values, string contains)
    {
        return values.Where((item) => item.Contains(contains));
    }


    [AsMethod(Name = "filter_contains")]
    public static IEnumerable<T> FilterContains<T>(AsExecutionContext ctx, IEnumerable<T> values, string propertyName, string contains)
    {
        var param = Expression.Parameter(typeof(T), "item");
        var propertyGetter = Expression.Property(param, propertyName);

        var valueGetter = ExprTreeHelper.GetExprToStringDelegate<T>(param, propertyGetter);

        return values.Where((item) => valueGetter(item).Contains(contains));
    }

    [AsMethod(Name = "filter_regex")]
    public static IEnumerable<T> FilterContainsRegex<T>(AsExecutionContext ctx, IEnumerable<T> values, string propertyName, string regexStr)
    {
            var param = Expression.Parameter(typeof(T), "item");
            var propertyGetter = Expression.Property(param, propertyName);

            var valueGetter = ExprTreeHelper.GetExprToStringDelegate<T>(param, propertyGetter);

            var regex = GetRegex(regexStr);

            return values.Where((item) => regex.IsMatch(valueGetter(item)));
        }

    [AsMethod(Name = "filter_regex")]
    public static IEnumerable<string> FilterContainsStringRegex(AsExecutionContext ctx, IEnumerable<string> values, string regexStr)
    {
            var regex = GetRegex(regexStr);

            return values.Where((item) => regex.IsMatch(item));
        }

    [AsMethod(Name = "filter_not_contains")]
    public static IEnumerable<T> FilterNotContains<T>(AsExecutionContext ctx, IEnumerable<T> values, string propertyName, string contains)
    {
            var param = Expression.Parameter(typeof(T), "item");
            var propertyGetter = Expression.Property(param, propertyName);

            var valueGetter = ExprTreeHelper.GetExprToStringDelegate<T>(param, propertyGetter);

            return values.Where((item) => !valueGetter(item).Contains(contains));
        }

    [AsMethod(Name = "filter_not_regex")]
    public static IEnumerable<T> FilterNotContainsRegex<T>(AsExecutionContext ctx, IEnumerable<T> values, string propertyName, string regexStr)
    {
            var param = Expression.Parameter(typeof(T), "item");
            var propertyGetter = Expression.Property(param, propertyName);

            var valueGetter = ExprTreeHelper.GetExprToStringDelegate<T>(param, propertyGetter);

            var regex = GetRegex(regexStr);

            return values.Where((item) => !regex.IsMatch(valueGetter(item)));
        }

    [AsMethod(Name = "filter_not_regex")]
    public static IEnumerable<string> FilterNotContainsStringRegex(AsExecutionContext ctx, IEnumerable<string> values, string regexStr)
    {
            var regex = GetRegex(regexStr);

            return values.Where((item) => !regex.IsMatch(item));
        }

    [AsMethod(Name = "filter_not_contains")]
    public static IEnumerable<string> FilterNotContainsString(AsExecutionContext ctx, IEnumerable<string> values, string contains)
    {
            return values.Where((item) => !item.Contains(contains));
        }

    [AsMethod(Name = "json")]
    public static string Json<T>(AsExecutionContext ctx, T obj) => JsonSerializer.Serialize(obj);

    [AsMethod(Name = "group")]
    [AsMethod(Name = "distinct")]
    public static HashSet<T> Group<T>(AsExecutionContext ctx, IEnumerable<T> values)
        => values.ToHashSet();

    [AsMethod(Name = "group")]
    [AsMethod(Name = "distinct")]
    public static HashSet<string> GroupString(AsExecutionContext ctx, IEnumerable<string> values)
        => values.ToHashSet();

    [AsMethod(Name = "limit")]
    [AsMethod(Name = "take")]
    public static IEnumerable<T> Take<T>(AsExecutionContext ctx, IEnumerable<T> source, int count) => source.Take(count);

        
    [AsMethod(Name = "skip")]
    public static IEnumerable<T> Skip<T>(AsExecutionContext ctx, IEnumerable<T> source, int count) => source.Skip(count);

        
    [AsMethod(Name = "split")]
    public static string[] Split(AsExecutionContext ctx, string source, string splitter) => source.Split(splitter);


    [AsMethod(Name = "not_null")]
    public static T ThrowIfNull<T>(AsExecutionContext ctx, T instance) => instance ?? throw new NullReferenceException();

    [AsMethod(Name = "not_empty")]
    public static IEnumerable<T> ThrowIfEmpty<T>(AsExecutionContext ctx, IEnumerable<T> instance) => instance.Any() ? instance : throw new NullReferenceException();

    [AsMethod(Name = "not_empty")]
    public static string ThrowIfEmptyString(AsExecutionContext ctx, string instance) => instance.Length == 0 ? instance : throw new NullReferenceException();

    [AsMethod(Name = "format")]
    public static string Format(AsExecutionContext ctx, string format)
    {
        return ctx.VariableContext.Interpolation(format, ctx.CurrentExecuteObject!.LexicalToken);
    }

    public static AsInterpreter RegisterBasicFunctionsV2(this AsInterpreter interpreter)
    {
        interpreter.Variables.Methods.ScanAndRegisterStaticFunction(typeof(BasicFunctionV2));
        return interpreter;
    }
}