﻿using AnalysisScript.Interpreter;
using AnalysisScript.Interpreter.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnalysisScript.Library
{
    public static class BasicFunctionV2
    {
        internal static Dictionary<string, Regex> RegexCache = [];

        internal static Regex GetRegex(string regexStr)
        {
            if (!RegexCache.TryGetValue(regexStr, out var regex))
            {
                RegexCache.Add(regexStr, regex = new Regex(regexStr));
            }
            return regex;
        }

        public static LambdaExpression Select<T>(AsExecutionContext ctx, T @this, string propertyName)
        {
            var paramGetter = ExprTreeHelper.GetConstantValueLambda(@this);
            var property = Expression.Property(paramGetter, propertyName);

            return Expression.Lambda(property);
        }

        public static string Join<T>(AsExecutionContext ctx, IEnumerable<T> values, string delimiter)
        {
            return string.Join(delimiter, values);
        }

        public static IEnumerable<T> FilterContains<T>(AsExecutionContext ctx, IEnumerable<T> values, string propertyName, string contains)
        {
            var param = Expression.Parameter(typeof(T), "item");
            var propertyGetter = Expression.Property(param, propertyName);

            var valueGetter = ExprTreeHelper.ExprToString<T>(param, propertyGetter);

            return values.Where((item) => valueGetter(item).Contains(contains));
        }

        public static IEnumerable<T> FilterContainsRegex<T>(AsExecutionContext ctx, IEnumerable<T> values, string propertyName, string regexStr)
        {
            var param = Expression.Parameter(typeof(T), "item");
            var propertyGetter = Expression.Property(param, propertyName);

            var valueGetter = ExprTreeHelper.ExprToString<T>(param, propertyGetter);

            var regex = GetRegex(regexStr);

            return values.Where((item) => regex.IsMatch(valueGetter(item)));
        }

        public static IEnumerable<string> FilterContainsStringRegex(AsExecutionContext ctx, IEnumerable<string> values, string regexStr)
        {
            var regex = GetRegex(regexStr);

            return values.Where((item) => regex.IsMatch(item));
        }

        public static IEnumerable<string> FilterContainsString(AsExecutionContext ctx, IEnumerable<string> values, string contains)
        {
            return values.Where((item) => item.Contains(contains));
        }


        public static IEnumerable<T> FilterNotContains<T>(AsExecutionContext ctx, IEnumerable<T> values, string propertyName, string contains)
        {
            var param = Expression.Parameter(typeof(T), "item");
            var propertyGetter = Expression.Property(param, propertyName);

            var valueGetter = ExprTreeHelper.ExprToString<T>(param, propertyGetter);

            return values.Where((item) => !valueGetter(item).Contains(contains));
        }

        public static IEnumerable<T> FilterNotContainsRegex<T>(AsExecutionContext ctx, IEnumerable<T> values, string propertyName, string regexStr)
        {
            var param = Expression.Parameter(typeof(T), "item");
            var propertyGetter = Expression.Property(param, propertyName);

            var valueGetter = ExprTreeHelper.ExprToString<T>(param, propertyGetter);

            var regex = GetRegex(regexStr);

            return values.Where((item) => !regex.IsMatch(valueGetter(item)));
        }

        public static IEnumerable<string> FilterNotContainsStringRegex(AsExecutionContext ctx, IEnumerable<string> values, string regexStr)
        {
            var regex = GetRegex(regexStr);

            return values.Where((item) => !regex.IsMatch(item));
        }

        public static IEnumerable<string> FilterNotContainsString(AsExecutionContext ctx, IEnumerable<string> values, string contains)
        {
            return values.Where((item) => !item.Contains(contains));
        }

        public static string Json<T>(AsExecutionContext ctx, T obj) => JsonSerializer.Serialize(obj);

        public static HashSet<T> Group<T>(AsExecutionContext ctx, IEnumerable<T> values)
            => values.ToHashSet();

        public static HashSet<string> GroupString(AsExecutionContext ctx, IEnumerable<string> values)
            => values.ToHashSet();

        public record struct Response(int code, string msg);
        public static async ValueTask<Response> GetUrl(AsExecutionContext executionContext, string @this)
        {
            using var req = new HttpClient();

            var res = await req.PostAsync("https://app.mokahr.com/success", null);

            return await res.Content.ReadFromJsonAsync<Response>();
        }

        public static T ThrowIfNull<T>(T instance) => instance ?? throw new NullReferenceException();
        public static IEnumerable<T> ThrowIfEmpty<T>(IEnumerable<T> instance) => !instance.Any() ? instance : throw new NullReferenceException();
        public static string ThrowIfEmptyString(string instance) => instance.Length == 0 ? instance : throw new NullReferenceException();

        public static AsInterpreter RegisterBasicFunctionsV2(this AsInterpreter interpreter)
        {
            interpreter.RegisterStaticFunction("select", typeof(BasicFunctionV2).GetMethod(nameof(Select)));
            interpreter.RegisterStaticFunction("join", typeof(BasicFunctionV2).GetMethod(nameof(Join)));

            interpreter.RegisterStaticFunction("filter_contains", typeof(BasicFunctionV2).GetMethod(nameof(FilterContains)));
            interpreter.RegisterStaticFunction("filter_contains", typeof(BasicFunctionV2).GetMethod(nameof(FilterContainsString)));

            interpreter.RegisterStaticFunction("filter_regex", typeof(BasicFunctionV2).GetMethod(nameof(FilterContainsRegex)));
            interpreter.RegisterStaticFunction("filter_regex", typeof(BasicFunctionV2).GetMethod(nameof(FilterContainsStringRegex)));

            interpreter.RegisterStaticFunction("filter_not_contains", typeof(BasicFunctionV2).GetMethod(nameof(FilterNotContains)));
            interpreter.RegisterStaticFunction("filter_not_contains", typeof(BasicFunctionV2).GetMethod(nameof(FilterNotContainsString)));

            interpreter.RegisterStaticFunction("filter_not_regex", typeof(BasicFunctionV2).GetMethod(nameof(FilterNotContainsRegex)));
            interpreter.RegisterStaticFunction("filter_not_regex", typeof(BasicFunctionV2).GetMethod(nameof(FilterNotContainsStringRegex)));

            interpreter.RegisterStaticFunction("group", typeof(BasicFunctionV2).GetMethod(nameof(Group)));
            interpreter.RegisterStaticFunction("group", typeof(BasicFunctionV2).GetMethod(nameof(GroupString)));

            interpreter.RegisterStaticFunction("distinct", typeof(BasicFunctionV2).GetMethod(nameof(Group)));
            interpreter.RegisterStaticFunction("distinct", typeof(BasicFunctionV2).GetMethod(nameof(GroupString)));

            interpreter.RegisterStaticFunction("not_null", typeof(BasicFunctionV2).GetMethod(nameof(ThrowIfNull)));

            interpreter.RegisterStaticFunction("not_empty", typeof(BasicFunctionV2).GetMethod(nameof(ThrowIfNull)));
            interpreter.RegisterStaticFunction("not_empty", typeof(BasicFunctionV2).GetMethod(nameof(ThrowIfEmptyString)));

            interpreter.RegisterStaticFunction("json", typeof(BasicFunctionV2).GetMethod(nameof(Json)));
            interpreter.RegisterStaticFunction("test_get_url", typeof(BasicFunctionV2).GetMethod(nameof(GetUrl)));
            return interpreter;
        }
    }
}
