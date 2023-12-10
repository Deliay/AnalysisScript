using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Interpreter.Variables
{
    public static class ExprTreeHelper
    {
        private readonly static Dictionary<Type, MethodInfo> TypeToString = [];
        private readonly static Dictionary<Type, Type> TypeToContainerType = [];
        private static Type GetContainerType(Type underlying)
        {
            if (!TypeToContainerType.TryGetValue(underlying, out var type))
            {
                TypeToContainerType.Add(underlying, type = typeof(Container<>).MakeGenericType(underlying));
            }

            return type;
        }
        private static MethodInfo GetToStringMethod(Type underlying)
        {
            if (!TypeToString.TryGetValue(underlying, out var method))
            {
                TypeToString.Add(underlying, method = underlying.GetMethod("ToString")!);
            }

            return method;
        }
        private static (ParameterExpression, MemberExpression) UnderlyingValueExpr(IContainer container)
        {
            var parameter = Expression.Parameter(typeof(IContainer), "container");

            var castedContainer = Expression.Convert(parameter, container.GetType());

            var valueProperty = Expression.Property(castedContainer, "Value");

            return (parameter, valueProperty);
        }

        public static MethodCallExpression GetConstantValueLambda(object? value)
        {
            var constant = Expression.Constant(value);

            var method = Expression.Lambda(constant).Compile();

            return Expression.Call(method.Method, Enumerable.Empty<Expression>());
        }

        public static MethodCallExpression GetContainerValueLambda(IContainer container)
        {
            var constant = Expression.Constant(container);

            var castedContainer = Expression.Convert(constant, container.GetType());

            var valueProperty = Expression.Property(castedContainer, "Value");

            var method = Expression.Lambda(valueProperty).Compile();
            
            return Expression.Call(method.Method, Enumerable.Empty<Expression>());
        }

        public static Func<string?> ExprToString(IContainer container)
        {
            var (parameter, value) = UnderlyingValueExpr(container);

            var callUnderlyingToString = Expression.Call(value, GetToStringMethod(container.UnderlyingType));

            var rawFunc = Expression.Lambda<Func<IContainer, string?>>(callUnderlyingToString, parameter).Compile();

            return () => rawFunc(container);
        }

        public static Func<object?> BoxUnderlyingValue(IContainer container)
        {
            var (parameter, value) = UnderlyingValueExpr(container);

            var rawFunc = Expression.Lambda<Func<IContainer, object?>>(value, parameter).Compile();

            return () => rawFunc(container);
        }

        public static Func<object, IContainer> UnboxToContainer(object instance)
        {
            var instType = instance.GetType();
            var type = GetContainerType(instType);

            var parameter = Expression.Parameter(typeof(object), "value");
            var castAs = Expression.TypeAs(parameter, instType);

            var ctor = type.GetConstructors()[0];
            var newContainer = Expression.New(ctor, castAs);

            var castToIContainer = Expression.TypeAs(newContainer, typeof(IContainer));

            return Expression.Lambda<Func<object, IContainer>>(castToIContainer, parameter).Compile();
        }

        public static string TypeParamString(Type type) => $"{type.FullName}";
        public static string JoinTypeParams(IEnumerable<string> paramStrings) => string.Join(',', paramStrings);

        public static (MethodCallExpression, string) BuildMethod(MethodInfo method)
        {
            var methodParams = method.GetParameters().Select(param => Expression.Parameter(param.ParameterType, param.Name));
            var methodSignatures = method.GetParameters().Select(param => TypeParamString(param.ParameterType));
            var callExpr = Expression.Call(null, method, methodParams);

            return (callExpr, JoinTypeParams(methodSignatures));
        }

        private static Dictionary<Type, MethodInfo> TypeToCastMethod = [];

        public static MethodInfo GetTypeToCastMethod(Type underlying)
        {
            if (!TypeToCastMethod.TryGetValue(underlying, out var method))
            {
                if (underlying.BaseType == typeof(ValueTask<>).BaseType)
                {
                    TypeToString.Add(underlying, method = typeof(ExprTreeHelper).GetMethod(nameof(AsyncValueTaskCast))!);
                }
                else if (underlying.BaseType == typeof(Task<>).BaseType)
                {
                    TypeToString.Add(underlying, method = typeof(ExprTreeHelper).GetMethod(nameof(AsyncTaskCast))!);
                }
                else
                {
                    TypeToString.Add(underlying, method = typeof(ExprTreeHelper).GetMethod(nameof(ValueTaskCast))!);
                }
            }

            return method;
        }
        public static ValueTask<IContainer> ValueTaskCast<T>(T old) => ValueTask.FromResult(IContainer.Of(old));
        public static async ValueTask<IContainer> AsyncValueTaskCast<T>(ValueTask<T> old) => IContainer.Of(await old);
        public static async ValueTask<IContainer> AsyncTaskCast<T>(Task<T> old) => IContainer.Of(await old);
    }
}
