using AnalysisScript.Interpreter;
using AnalysisScript.Interpreter.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Library
{
    public static class BasicFunctionV2
    {

        public static LambdaExpression Select<T>(AsExecutionContext ctx, T @this, string propertyName)
        {
            var paramGetter = ExprTreeHelper.GetConstantValueLambda(@this);
            var property = Expression.Property(paramGetter, propertyName);

            return Expression.Lambda(property);
        }

        public static IEnumerable<T> FilterContains<T>(AsExecutionContext ctx, IEnumerable<T> values, string propertyName, string contains)
        {
            var param = Expression.Parameter(typeof(T), "item");
            var propertyGetter = Expression.Property(param, propertyName);

            var valueGetter = ExprTreeHelper.ExprToString<T>(param, propertyGetter);

            return values
                .Where((item) => valueGetter(item).Contains(contains));
        }


        public static string Join<T>(AsExecutionContext ctx, IEnumerable<T> values, string delimiter)
        {
            return string.Join(delimiter, values);
        }

        public static IEnumerable<string> FilterContainsString(AsExecutionContext ctx, IEnumerable<string> values, string contains)
        {
            return values
                .Where((item) => item.Contains(contains));
        }

        public static AsInterpreter RegisterBasicFunctionsV2(this AsInterpreter interpreter)
        {
            interpreter.RegisterFunction("select", typeof(BasicFunctionV2).GetMethod(nameof(Select)));
            interpreter.RegisterFunction("join", typeof(BasicFunctionV2).GetMethod(nameof(Join)));
            interpreter.RegisterFunction("filter_contains", typeof(BasicFunctionV2).GetMethod(nameof(FilterContains)));
            interpreter.RegisterFunction("filter_contains", typeof(BasicFunctionV2).GetMethod(nameof(FilterContainsString)));
            return interpreter;
        }
    }
}
