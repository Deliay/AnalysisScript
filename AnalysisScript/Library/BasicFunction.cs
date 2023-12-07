using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using AnalysisScript.Interpreter;

namespace AnalysisScript.Library;

public static class BasicFunction
{

    internal static Dictionary<Type, Dictionary<string, Func<object, object>>> GetterMethodCache = [];
    internal static Dictionary<string, Regex> RegexCache = [];

    internal static Regex GetRegex(string regexStr)
    {
        if (!RegexCache.TryGetValue(regexStr, out var regex))
        {
            RegexCache.Add(regexStr, regex = new Regex(regexStr));
        }
        return regex;
    }

    internal static object GetValue(object obj, string name)
    {
        var type = obj.GetType();
        if (!GetterMethodCache.TryGetValue(type, out var typeCached))
        {
            typeCached = [];
            GetterMethodCache.Add(type, typeCached);
        }
        if (!typeCached.TryGetValue(name, out var cached))
        {
            var prop = type.GetProperty(name);
            if (prop != null) {
                typeCached.Add(name, cached = (o) => prop.GetValue(o));
            }
            var field = type.GetField(name);
            if (field != null) {
                typeCached.Add(name, cached = (o) => field.GetValue(o));
            }
            if (cached == null) throw new MissingFieldException(name);
        }

        return cached(obj);
    }
    public static ValueTask<object> FilterContains(object values, object[] @params)
    {
        Assert.Count(1, @params);
        if (@params.Length == 1)
        {
            Assert.IsSeq<string>(values, out var strings);
            return ValueTask.FromResult<object>(strings.Where(str => str.Contains(@params[0].ToString())));
        }
        else
        {
            var proeprty = @params[0].ToString();
            var value = @params[1].ToString();
            
            Assert.IsSeq(values, out var objects);
            return ValueTask.FromResult<object>(objects.Cast<object>().Where(obj => GetValue(obj, proeprty)?.ToString()?.Contains(value) ?? false));
        }
    }
    public static ValueTask<object> FilterRegex(object values, object[] @params)
    {
        Assert.Count(1, @params);
        if (@params.Length == 1)
        {
            var regex = GetRegex(@params[0].ToString());
            Assert.IsSeq<string>(values, out var strings);
            return ValueTask.FromResult<object>(strings.Where(str => regex.IsMatch(str)));
        }
        else
        {
            var proeprty = @params[0].ToString();
            var regex = GetRegex(@params[1].ToString());
            
            Assert.IsSeq(values, out var objects);
            return ValueTask.FromResult<object>(objects.Cast<object>()
                .Where(obj => regex.IsMatch(GetValue(obj, proeprty)?.ToString())));
        }
    }

    public static ValueTask<object> Group(object values, object[] @params)
    {
        if (@params.Length == 0)
        {
            Assert.Is<IEnumerable<string>>(values, out var strings);
            return ValueTask.FromResult<object>(strings.ToHashSet());
        }
        
        var property = @params[0].ToString();
        Assert.IsSeq(values, out IEnumerable objects);
        return ValueTask.FromResult<object>(objects.Cast<object>().GroupBy(k => GetValue(k, property)));
    }

    public static ValueTask<object> Select(object values, object[] @params)
    {
        Assert.Count(1, @params);
        
        var property = @params[0].ToString();

        Assert.IsSeq(values, out var objects);
        return ValueTask.FromResult<object>(objects.Cast<object>().Select(obj => GetValue(obj, property)));
    }

    public static ValueTask<object> Join(object values, object[] @params)
    {
        Assert.Count(1, @params);

        var delimiter = @params[0].ToString();

        Assert.IsSeq(values, out var objects);
        return ValueTask.FromResult<object>(string.Join(delimiter, objects.Cast<object>()));
    }

    public static AsInterpreter RegisterBasicFunctions(this AsInterpreter interpreter)
    {
        interpreter.RegisterFunction("filter_contains", FilterContains);
        interpreter.RegisterFunction("filter_regex", FilterRegex);
        interpreter.RegisterFunction("group", Group);
        interpreter.RegisterFunction("select", Select);
        interpreter.RegisterFunction("join", Join);
        return interpreter;
    }
}
