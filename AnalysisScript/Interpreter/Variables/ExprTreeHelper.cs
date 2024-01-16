using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using AnalysisScript.Library;

namespace AnalysisScript.Interpreter.Variables;

public static class ExprTreeHelper
{
    private static Delegate ToDelegate(Delegate @delegate) => @delegate;
    
    private static readonly Dictionary<Type, MethodInfo> TypeToString = [];
    private static readonly Dictionary<Type, Type> TypeToContainerType = [];
    private static Type GetContainerType(Type underlying)
    {
        if (!TypeToContainerType.TryGetValue(underlying, out var type))
        {
            TypeToContainerType.Add(underlying, type = typeof(Container<>).MakeGenericType(underlying));
        }

        return type;
    }
    private class InternEnumerableToStringHelper<TContainer, TItem> where TContainer : IEnumerable<TItem>
    {
        public static string CollectionToString(TContainer container)
        {
            return $"{string.Join(',', container.Take(3))}...共{container.Count()}项";
        }
    }
    private static readonly Dictionary<Type, MethodInfo> EnumerableToStringMethods = [];
    private static MethodInfo GetEnumerableToStringMethod(Type underlying)
    {
        if (EnumerableToStringMethods.TryGetValue(underlying, out var method)) return method;

        var type = typeof(InternEnumerableToStringHelper<,>)
            .MakeGenericType(
                underlying,
                underlying.GenericTypeArguments[0]);
        EnumerableToStringMethods.Add(underlying, method = type.GetMethod("CollectionToString")!);

        return method;
    }

    private static MethodInfo GetToStringMethod(Type underlying)
    {
        if (TypeToString.TryGetValue(underlying, out var method)) return method;
        
        if (underlying.GetMethod("ToString", []) is { } typeToString && typeToString.DeclaringType.GUID == underlying.GUID)
        {
            TypeToString.Add(underlying, method = typeToString);
        }
        else if (FindStableType(typeof(IEnumerable<>), underlying) is { } stableIEnumerable)
        {
            return null!;
        }
        else 
            TypeToString.Add(underlying, method = typeof(object).GetMethod("ToString")!);

        return method;
    }
    private static readonly ParameterExpression ContainerParameter = Expression.Parameter(typeof(IContainer), "container");
    private static readonly Dictionary<Type, UnaryExpression> UnderlyingCastExprCache = [];
    private static readonly Dictionary<Type, MemberExpression> UnderlyingPropertyCache = [];
    private static (ParameterExpression, MemberExpression) UnderlyingValueExpr(IContainer container)
    {
        var type = container.GetType();
        if (!UnderlyingCastExprCache.TryGetValue(type, out var castedContainer))
        {
            UnderlyingCastExprCache.Add(type, castedContainer = Expression.Convert(ContainerParameter, type));
        }

        if (!UnderlyingPropertyCache.TryGetValue(type, out var valueProperty))
        {
            UnderlyingPropertyCache.Add(type, valueProperty = Expression.Property(castedContainer, "Value"));
        }

        return (ContainerParameter, valueProperty);
    }

    public static MethodCallExpression GetConstantValueLambda<T>(T? value)
    {
        return GetContainerValueLambda(IContainer.Of(value));
    }
    public static MethodCallExpression GetConstantValueLambda(IContainer value)
    {
        return GetContainerValueLambda(value);
    }

    private static readonly Dictionary<Type, MethodInfo> BoxExprMethods = [];

    private static readonly MethodInfo RawBoxMethod = ToDelegate(IContainer.Of<int>)
        .Method.GetGenericMethodDefinition();
    private static MethodInfo GetBoxExprToContainerMethod(Type type)
    {
        if (BoxExprMethods.TryGetValue(type, out var method)) return method;
        
        BoxExprMethods.Add(type, method = RawBoxMethod.MakeGenericMethod(type));

        return method;
    }
    public static IContainer GetConstantValueLambda(Type type, IEnumerable<MethodCallExpression> container)
    {
        var array = Expression.NewArrayInit(type, container);
        var arrayType = array.Type;
        
        var method = GetBoxExprToContainerMethod(arrayType);
        var boxCall = Expression.Call(method, array);
        var idCall = Expression.Call(MakeIdentity(typeof(IContainer), false), boxCall);
        return Expression.Lambda<Func<IContainer>>(idCall).Compile()();
    }

    public static Delegate GetIdentityLambda(Type type) {
        var parameter = Expression.Parameter(type);

        return Expression.Lambda(parameter, parameter).Compile();
    }

    public static T ContainerIdentity<T>(Container<T> instance) => instance.Value;
    public static T Identity<T>(T instance) => instance;
    
    private static readonly Dictionary<Type, MethodInfo> ContainerIdentityMethods = [];
    private static readonly MethodInfo ContainerIdentityMethod = ToDelegate(ContainerIdentity<int>)
        .Method.GetGenericMethodDefinition();
    private static readonly MethodInfo IdentityMethod = ToDelegate(Identity<int>)
        .Method.GetGenericMethodDefinition();

    private static MethodInfo MakeIdentity(Type type, bool containerized = true)
    {
        if (ContainerIdentityMethods.TryGetValue(type, out var method)) return method;

        method = containerized
            ? ContainerIdentityMethod.MakeGenericMethod(type)
            : IdentityMethod.MakeGenericMethod(type);
        
        ContainerIdentityMethods.Add(type, method);
        
        return method;
    }

    public static MethodCallExpression GetContainerValueLambda(IContainer container)
    {
        var constant = Expression.Constant(container);

        var id = MakeIdentity(container.UnderlyingType);
        var invoke = Expression.Call(id, constant);

        return invoke;
    }

    public static Func<T, string> ExprToString<T>(ParameterExpression parameter, MemberExpression member)
    {
        var lambda = Expression.Lambda(member);

        var invoke = Expression.Invoke(lambda);

        var method = GetToStringMethod(member.Type);
        var callUnderlyingToString = method == null
            ? Expression.Call(GetEnumerableToStringMethod(member.Type), invoke)
            : Expression.Call(invoke, method);

        var toString = Expression.Lambda<Func<T, string>>(callUnderlyingToString, parameter);

        return toString.Compile();
    }

    public static string? ExprToString(IContainer container)
    {
        var (parameter, valueGetter) = UnderlyingValueExpr(container);

        var lambda = Expression.Lambda(valueGetter);

        var invoke = Expression.Invoke(lambda);

        var method = GetToStringMethod(container.UnderlyingType);
        var callUnderlyingToString = method is null
            ? Expression.Call(GetEnumerableToStringMethod(container.UnderlyingType), invoke)
            : Expression.Call(invoke, method);

        var toString = Expression.Lambda<Func<IContainer, string>>(callUnderlyingToString, parameter);

        return toString.Compile()(container);
    }

    public static Func<object?> BoxUnderlyingValue(IContainer container)
    {
        var (parameter, value) = UnderlyingValueExpr(container);

        var rawFunc = Expression.Lambda<Func<IContainer, object?>>(value, parameter).Compile();

        return () => rawFunc(container);
    }

    private static readonly Dictionary<(Type, Type), Delegate> _UnderlyingCastCache = [];
    public static async ValueTask<Func<T>> ValueCastTo<T>(IContainer container)
    {

        var sanitizedContainer = await SanitizeLambdaExpression(container);
        
        var (parameter, value) = UnderlyingValueExpr(sanitizedContainer);
        
        if (!value.Type.IsAssignableTo(typeof(T)))
            throw new InvalidCastException($"{value.Type} can't cast to {typeof(T)}");

        if (!_UnderlyingCastCache.TryGetValue((value.Type, typeof(T)), out var rawConverter))
        {
            if (value.Type.IsValueType)
            {
                var valueTypeExpr = Expression.Lambda<Func<IContainer, T>>(value, parameter).Compile();

                _UnderlyingCastCache.Add((value.Type, typeof(T)), rawConverter = valueTypeExpr);
            }
            else
            {
                var cast = Expression.TypeAs(value, typeof(T));

                var rawFunc = Expression.Lambda<Func<IContainer, T>>(cast, parameter).Compile();

                _UnderlyingCastCache.Add((value.Type, typeof(T)), rawConverter = rawFunc);
            }
        }
        var method = (Func<IContainer, T>)rawConverter;
        return () => method(sanitizedContainer);
    }


    public static IContainer SanitizeIdentityMethodCallExpression(MethodCallExpression expr)
    {
        if (!(expr.Method == MakeIdentity(expr.Method.ReturnType)))
            throw new InvalidCastException("Can't sanitize non Identity MethodCallExpression");

        return Expression.Lambda<Func<IContainer>>(expr).Compile()();
    }
    
    public static async ValueTask<IContainer> SanitizeLambdaExpression(IContainer value)
    {
        if (value.UnderlyingType == typeof(LambdaExpression)) {
            var expr = value.As<LambdaExpression>()!;
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
        foreach (var candidate in interfaces)
        {
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
        var parameterizedTypes = genericImplType.GetGenericArguments();

        for (var i = 0; i < genericTypes.Length; i++)
        {
            yield return (genericTypes[i], parameterizedTypes[i]);
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

        Dictionary<Type, Type> genericTypeToTargetType = [];
        for (var i = 0; i < methodParameters.Length; i++)
        {
            var genericParam = methodParameters[i];
            var orderedParam = orderedParameter[i];
            if (!genericParam.ParameterType.ContainsGenericParameters) continue;
    
            if (genericTypeToTargetType.ContainsKey(genericParam.ParameterType)) continue;

            if (genericParam.ParameterType.IsGenericParameter)
            {
                genericTypeToTargetType.Add(genericParam.ParameterType, orderedParam);
            }
            else
            {
                foreach (var (a, b) in ExtractTypeMapping(genericParam.ParameterType, orderedParam))
                {
                    genericTypeToTargetType.Add(a, b);
                }
            }
        }
            
        if (genericTypes.Any(type => !genericTypeToTargetType.ContainsKey(type))) {
            return false;
        }

        method = genericMethod.MakeGenericMethod(genericTypes.Select((type) => genericTypeToTargetType[type]).ToArray());

        return true;
    }

    public static bool SignatureMatch(List<Type> from, List<Type> to)
    {
        if (from.Count != to.Count) return false;

        for (var i = 0; i < from.Count; i++)
        {
            var fromType = from[i];
            var toType = to[i];

            if (!toType.IsAssignableFrom(fromType)) return false;
        }

        return true;
    }

    public static (MethodCallExpression, string) BuildMethod(List<(MethodInfo, ConstantExpression?)> methods, IEnumerable<MethodCallExpression> parameters)
    {
        var parameterArray = parameters.ToList();
        var signature = GetSignatureTypesOf(parameterArray);
        methods.Sort((a, b) => a.Item1.IsGenericMethodDefinition ? 1 : -1);
        foreach (var (method, instance) in methods)
        {
            var currentMethod = method;
            if (method.IsGenericMethodDefinition)
            {
                if (!TryBuildGenericMethod(currentMethod, parameterArray, out currentMethod)) continue;
            }

            var methodParamSignatures = currentMethod.GetParameters().Select(param => param.ParameterType).ToList();

            if (!SignatureMatch(signature, methodParamSignatures)) continue;
            var methodParams = currentMethod.GetParameters().Select(param => Expression.Parameter(param.ParameterType, param.Name));
            var callExpr = Expression.Call(instance, currentMethod, methodParams);

            return (callExpr, GetSignatureOf(parameterArray));
        }
        throw new MissingMethodException($"No method match argument signature {signature}");

    }

    private static readonly Dictionary<Type, MethodInfo> TypeToCastMethod = [];

    public static MethodInfo GetTypeToCastMethod(Type underlying)
    {
        if (TypeToCastMethod.TryGetValue(underlying, out var method)) return method;

        if (underlying.GUID == typeof(ValueTask<>).GUID)
        {
            var currentTaskResultType = underlying.GenericTypeArguments[0];
            TypeToCastMethod.Add(underlying, method = AsyncValueTaskCastMethod.MakeGenericMethod(currentTaskResultType)!);
        }
        else if (underlying.GUID == typeof(Task<>).GUID)
        {
            var currentTaskResultType = underlying.GenericTypeArguments[0];
            TypeToCastMethod.Add(underlying, method = AsyncTaskCastMethod.MakeGenericMethod(currentTaskResultType)!);
        }
        else
        {
            TypeToCastMethod.Add(underlying, method = ValueTaskCastMethod.MakeGenericMethod(underlying)!);
        }

        return method;
    }
    public static ValueTask<IContainer> ValueTaskCast<T>(T old) => ValueTask.FromResult(IContainer.Of(old));

    private static readonly MethodInfo ValueTaskCastMethod = ToDelegate(ValueTaskCast<int>)
        .Method.GetGenericMethodDefinition();
    public static async ValueTask<IContainer> AsyncValueTaskCast<T>(ValueTask<T> old) => IContainer.Of(await old);

    private static readonly MethodInfo AsyncValueTaskCastMethod = ToDelegate(AsyncValueTaskCast<int>)
        .Method.GetGenericMethodDefinition();
    public static async ValueTask<IContainer> AsyncTaskCast<T>(Task<T> old) => IContainer.Of(await old);

    private static readonly MethodInfo AsyncTaskCastMethod = ToDelegate(AsyncTaskCast<int>)
        .Method.GetGenericMethodDefinition();
}