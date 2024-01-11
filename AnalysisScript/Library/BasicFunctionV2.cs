﻿using AnalysisScript.Interpreter;
using AnalysisScript.Interpreter.Variables;
using AnalysisScript.Interpreter.Variables.Method;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Reflection;
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

        [AsMethod(Name = "select")]
        public static LambdaExpression SelectSingle<T>(AsExecutionContext ctx, T @this, string propertyName)
        {
            var paramGetter = ExprTreeHelper.GetConstantValueLambda(@this);
            var property = Expression.Property(paramGetter, propertyName);

            return Expression.Lambda(property);
        }

        private readonly static MethodInfo SelectMethod = typeof(BasicFunctionV2).GetMethod("SelectWrapper")
            ?? throw new InvalidProgramException("Can't find Select(IEnumerable<>, Func<,>) method from Enumerable class");

        public static IEnumerable<R> SelectWrapper<T, R>(IEnumerable<T> source, Func<T, R> mapper) => source.Select(mapper);

        [AsMethod(Name = "select")]
        public static LambdaExpression SelectSequence<T>(AsExecutionContext ctx, IEnumerable<T> @this, string propertyName)
        {
            var param = Expression.Parameter(typeof(T));
            var property = Expression.Property(param, propertyName);

            var thisParam = ExprTreeHelper.GetConstantValueLambda(@this);

            var mapperMethod = Expression.Lambda(property, param);
            var selectMethod = SelectMethod.MakeGenericMethod([typeof(T), property.Type]);

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

            var valueGetter = ExprTreeHelper.ExprToString<T>(param, propertyGetter);

            return values.Where((item) => valueGetter(item).Contains(contains));
        }

        [AsMethod(Name = "filter_regex")]
        public static IEnumerable<T> FilterContainsRegex<T>(AsExecutionContext ctx, IEnumerable<T> values, string propertyName, string regexStr)
        {
            var param = Expression.Parameter(typeof(T), "item");
            var propertyGetter = Expression.Property(param, propertyName);

            var valueGetter = ExprTreeHelper.ExprToString<T>(param, propertyGetter);

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

            var valueGetter = ExprTreeHelper.ExprToString<T>(param, propertyGetter);

            return values.Where((item) => !valueGetter(item).Contains(contains));
        }

        [AsMethod(Name = "filter_not_regex")]
        public static IEnumerable<T> FilterNotContainsRegex<T>(AsExecutionContext ctx, IEnumerable<T> values, string propertyName, string regexStr)
        {
            var param = Expression.Parameter(typeof(T), "item");
            var propertyGetter = Expression.Property(param, propertyName);

            var valueGetter = ExprTreeHelper.ExprToString<T>(param, propertyGetter);

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

        public record struct Response(int code, string msg);
        
        [AsMethod(Name = "post")]
        public static async ValueTask<Response> Post<T>(AsExecutionContext executionContext, T body, string address)
        {
            using var req = new HttpClient();

            var res = await req.PostAsJsonAsync("https://app.mokahr.com/success", body);

            return await res.Content.ReadFromJsonAsync<Response>();
        }
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
        public static IEnumerable<T> ThrowIfEmpty<T>(AsExecutionContext ctx, IEnumerable<T> instance) => !instance.Any() ? instance : throw new NullReferenceException();

        [AsMethod(Name = "not_empty")]
        public static string ThrowIfEmptyString(AsExecutionContext ctx, string instance) => instance.Length == 0 ? instance : throw new NullReferenceException();

        public static AsInterpreter RegisterBasicFunctionsV2(this AsInterpreter interpreter)
        {
            interpreter.Variables.Methods.ScanAndRegisterStaticFunction(typeof(BasicFunctionV2));
            return interpreter;
        }
    }
}
