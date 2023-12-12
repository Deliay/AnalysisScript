using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public static Func<T, string> ExprToString<T>(ParameterExpression parameter, MemberExpression member)
        {
            var lambda = Expression.Lambda(member);

            var invoke = Expression.Invoke(lambda);

            var callUnderlyingToString = Expression.Call(invoke, GetToStringMethod(member.Type));

            var toString = Expression.Lambda<Func<T, string>>(callUnderlyingToString, parameter);

            return toString.Compile();
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
        public static List<Type> GetSignatureTypesOf(IEnumerable<MethodCallExpression> parameters)
        {
            return parameters.Select(getter => getter.Method.ReturnType).ToList();
        }

        private static Type? FindStableType(Type target, Type source)
        {
            if (source.GUID == target.GUID) return source;

            var interfaces = source.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                var candidate = interfaces[i];

                if (candidate.GUID == target.GUID) return candidate;
                
                var childStableType = FindStableType(target, candidate);

                if (childStableType is not null) return childStableType;
            }
            return null!;
        }

        private static IEnumerable<(Type, Type)> ExtractTypeMapping(Type generic, Type parameter)
        {
            var genericImplType = FindStableType(generic, parameter);

            if (genericImplType is null) yield break;
                
            var genericTypes = generic.GetGenericArguments();
            var parameteredTypes = genericImplType.GetGenericArguments();

            for (int i = 0; i < genericTypes.Length; i++)
            {
                yield return (genericTypes[i], parameteredTypes[i]);
            }
        }

        public static bool TryBuildGenericMethod(MethodInfo genericMethod, IEnumerable<MethodCallExpression> parameterGetters, [NotNullWhen(true)]out MethodInfo? method)
        {
            var genericTypes = genericMethod.GetGenericArguments();
            var methodParameters = genericMethod.GetParameters();

            var orderedParameter = parameterGetters.Select(getter => getter.Method.ReturnType).ToArray();

            method = null!;

            if (methodParameters.Length != orderedParameter.Length)
                return false;

            Dictionary<Type, Type> GenericTypeToTargetType = [];
            for (int i = 0; i < methodParameters.Length; i++)
            {
                var genericParam = methodParameters[i];
                var orderedParam = orderedParameter[i];
                if (!genericParam.ParameterType.ContainsGenericParameters) continue;
    
                if (GenericTypeToTargetType.ContainsKey(genericParam.ParameterType)) continue;

                if (genericParam.ParameterType.IsGenericParameter)
                {
                    GenericTypeToTargetType.Add(genericParam.ParameterType, orderedParam);
                }
                else
                {
                    foreach (var (a, b) in ExtractTypeMapping(genericParam.ParameterType, orderedParam))
                    {
                        GenericTypeToTargetType.Add(a, b);
                    }
                }
            }
            
            if (genericTypes.Any(type => !GenericTypeToTargetType.ContainsKey(type))) {
                return false;
            }

            method = genericMethod.MakeGenericMethod(genericTypes.Select((type) => GenericTypeToTargetType[type]).ToArray());

            return true;
        }

        public static bool SignatureMatch(List<Type> from, List<Type> to)
        {
            if (from.Count != to.Count) return false;

            for (int i = 0; i < from.Count; i++)
            {
                var fromType = from[i];
                var toType = to[i];

                if (!toType.IsAssignableFrom(fromType)) return false;
            }

            return true;
        }

        public static (MethodCallExpression, string) BuildMethod(List<(MethodInfo, ConstantExpression?)> methods, IEnumerable<MethodCallExpression> parameters)
        {
            var signature = GetSignatureTypesOf(parameters);
            methods.Sort((a, b) => { return a.Item1.IsGenericMethodDefinition ? 1 : -1; });
            foreach (var (method, instance) in methods)
            {
                var currentMethod = method;
                if (method.IsGenericMethodDefinition)
                {
                    if (!TryBuildGenericMethod(currentMethod, parameters, out currentMethod)) continue;
                }

                var methodParamSignatures = currentMethod.GetParameters().Select(param => param.ParameterType).ToList();

                if (!SignatureMatch(signature, methodParamSignatures)) continue;
                var methodParams = currentMethod.GetParameters().Select(param => Expression.Parameter(param.ParameterType, param.Name));
                var callExpr = Expression.Call(instance, currentMethod, methodParams);

                return (callExpr, GetSignatureOf(parameters));
            }
            throw new MissingMethodException($"No method match argument signature {signature}");

        }

        private static Dictionary<Type, MethodInfo> TypeToCastMethod = [];

        public static MethodInfo GetTypeToCastMethod(Type underlying)
        {
            if (!TypeToCastMethod.TryGetValue(underlying, out var method))
            {
                if (underlying.GUID == typeof(ValueTask<>).GUID)
                {
                    var currentTaskResultType = underlying.GenericTypeArguments[0];
                    TypeToCastMethod.Add(underlying, method = typeof(ExprTreeHelper).GetMethod(nameof(AsyncValueTaskCast)).MakeGenericMethod(currentTaskResultType)!);
                }
                else if (underlying.GUID == typeof(Task<>).GUID)
                {
                    var currentTaskResultType = underlying.GenericTypeArguments[0];
                    TypeToCastMethod.Add(underlying, method = typeof(ExprTreeHelper).GetMethod(nameof(AsyncTaskCast)).MakeGenericMethod(currentTaskResultType)!);
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
