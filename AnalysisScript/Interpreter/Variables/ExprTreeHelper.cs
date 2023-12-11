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
                TypeToString.Add(underlying, method = underlying.GetRuntimeMethod("ToString", [])!);
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

        public static MethodCallExpression GetConstantValueLambda<T>(T? value)
        {
            return GetContainerValueLambda(IContainer.Of(value));
        }

        public static Delegate Indentity(Type type) {
            var parameter = Expression.Parameter(type);

            return Expression.Lambda(parameter, parameter).Compile();
        }

        public static T Identity<T>(Container<T> instance) => instance.Value;
        private readonly static Dictionary<Type, MethodInfo> IndentityMethods = [];
        private static MethodInfo MakeIndentity(Type type)
        {
            if (!IndentityMethods.TryGetValue(type, out var method))
            {
                var selfType = typeof(ExprTreeHelper);
                var genericMethod = selfType.GetMethod(nameof(Identity));
                IndentityMethods.Add(type, method = genericMethod.MakeGenericMethod(type));
            }

            return method;
        }

        public static MethodCallExpression GetContainerValueLambda(IContainer container)
        {
            var constant = Expression.Constant(container);

            var id = MakeIndentity(container.UnderlyingType);
            var invoke = Expression.Call(id, constant);

            return invoke;
        }

        public static string? ExprToString(IContainer container)
        {
            var (parameter, valueGetter) = UnderlyingValueExpr(container);

            var lambda = Expression.Lambda(valueGetter);

            var invoke = Expression.Invoke(lambda);

            var callUnderlyingToString = Expression.Call(invoke, GetToStringMethod(container.UnderlyingType));

            var toString = Expression.Lambda<Func<IContainer, string>>(callUnderlyingToString, parameter);

            return toString.Compile()(container);
        }

        public static Func<object?> BoxUnderlyingValue(IContainer container)
        {
            var (parameter, value) = UnderlyingValueExpr(container);

            var rawFunc = Expression.Lambda<Func<IContainer, object?>>(value, parameter).Compile();

            return () => rawFunc(container);
        }

        public static IContainer SanitizeIdentityMethodCallExpression(MethodCallExpression expr)
        {
            if (!(expr.Method == MakeIndentity(expr.Method.ReturnType)))
                throw new InvalidCastException("Can't sanitize non Identity MethodCallExpression");

            return Expression.Lambda<Func<IContainer>>(expr).Compile()();
        }

        public static async ValueTask<IContainer> SanitizeLambdaExpression(IContainer value)
        {
            if (value.UnderlyingType == typeof(LambdaExpression)) {
                var expr = value.As<LambdaExpression>();
                var invoke = Expression.Invoke(expr);

                var retContainerType = GetContainerType(expr.ReturnType);
                var retContainerTypeCtor = retContainerType.GetConstructors()[0];
                var wrappedValue = Expression.Call(GetTypeToCastMethod(expr.ReturnType), invoke);
                
                var valueLambda = Expression.Lambda<Func<ValueTask<IContainer>>>(wrappedValue);
                var valueGetter = valueLambda.Compile();
                return await valueGetter();
            } else {
                return value;
            }
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

        public static string GetSignatureOf(IEnumerable<MethodCallExpression> parameters)
        {
            var methodSignatures = parameters.Select(getter => TypeParamString(getter.Method.ReturnType));
            return JoinTypeParams(methodSignatures);
        }

        public static (MethodCallExpression, string) BuildGenericMethod(MethodInfo genericMethod, IEnumerable<MethodCallExpression> parameterGetters)
        {
            var genericTypes = genericMethod.GetGenericArguments();
            var methodParameters = genericMethod.GetParameters();

            var orderedParameter = parameterGetters.Select(getter => getter.Method.ReturnType).ToArray();
            
            if (methodParameters.Length != orderedParameter.Length)
                throw new MissingMethodException($"Provided parameters doesn't match generic method {genericMethod.Name}");

            Dictionary<Type, Type> GenericTypeToTargetType = [];
            for (int i = 0; i < methodParameters.Length; i++)
            {
                var genericParam = methodParameters[i];
                if (!genericParam.ParameterType.IsGenericMethodParameter) continue;
    
                if (GenericTypeToTargetType.ContainsKey(genericParam.ParameterType)) continue;

                GenericTypeToTargetType.Add(genericParam.ParameterType, orderedParameter[i]);
            }
            
            if (genericTypes.Any(type => !GenericTypeToTargetType.ContainsKey(type)))
                throw new MissingMethodException($"Generic type inference doesn't support now, use LambdaExpression for instead");

            var method = genericMethod.MakeGenericMethod(genericTypes.Select((type) => GenericTypeToTargetType[type]).ToArray());

            return BuildMethod(method, parameterGetters);
        }

        public static (MethodCallExpression, string) BuildMethod(MethodInfo method, IEnumerable<MethodCallExpression> parameters)
        {
            var signature = GetSignatureOf(parameters);

            if (method.IsGenericMethodDefinition)
            {
                return BuildGenericMethod(method, parameters);
            }

            var methodParams = method.GetParameters().Select(param => Expression.Parameter(param.ParameterType, param.Name));
            var methodParamSignatures = method.GetParameters().Select(param => TypeParamString(param.ParameterType));
            var methodSignature = JoinTypeParams(methodParamSignatures);

            if (signature != methodSignature)
                throw new MissingMethodException($"method {method.Name} doesn't match argument signature {signature}");

            var callExpr = Expression.Call(null, method, methodParams);

            return (callExpr, methodSignature);
        }

        private static Dictionary<Type, MethodInfo> TypeToCastMethod = [];

        public static MethodInfo GetTypeToCastMethod(Type underlying)
        {
            if (!TypeToCastMethod.TryGetValue(underlying, out var method))
            {
                if (underlying.GUID == typeof(ValueTask<>).GUID)
                {
                    TypeToCastMethod.Add(underlying, method = typeof(ExprTreeHelper).GetMethod(nameof(AsyncValueTaskCast)).MakeGenericMethod(underlying)!);
                }
                else if (underlying.GUID == typeof(Task<>).GUID)
                {
                    TypeToCastMethod.Add(underlying, method = typeof(ExprTreeHelper).GetMethod(nameof(AsyncTaskCast)).MakeGenericMethod(underlying)!);
                }
                else
                {
                    TypeToCastMethod.Add(underlying, method = typeof(ExprTreeHelper).GetMethod(nameof(ValueTaskCast)).MakeGenericMethod(underlying)!);
                }
            }

            return method;
        }
        public static ValueTask<IContainer> ValueTaskCast<T>(T old) => ValueTask.FromResult(IContainer.Of(old));
        public static async ValueTask<IContainer> AsyncValueTaskCast<T>(ValueTask<T> old) => IContainer.Of(await old);
        public static async ValueTask<IContainer> AsyncTaskCast<T>(Task<T> old) => IContainer.Of(await old);
    }
}
