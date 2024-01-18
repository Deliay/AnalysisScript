using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace AnalysisScript.Interpreter.Variables;

public static class ExprTreeHelper
{
    private static readonly MethodInfo SelectMethod = ToDelegate(SelectWrapper<int, int>)
        .Method.GetGenericMethodDefinition();

    private static readonly Dictionary<(Type, Type), MethodInfo> ConstructedSelectMethod = [];

    public static MethodInfo ConstructSelectMethod(Type from, Type to)
    {
        if (!ConstructedSelectMethod.TryGetValue((from, to), out var method))
        {
            ConstructedSelectMethod.Add((from, to), method = SelectMethod.MakeGenericMethod(from ,to));
        }

        return method;
    }
    
    private static IEnumerable<TOut> SelectWrapper<TIn, TOut>(IEnumerable<TIn> source, Func<TIn, TOut> mapper)
        => source.Select(mapper);

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
            return $"{string.Join(',', container.Take(3))}...Total {container.Count()} item(s)";
        }

    }
    private static readonly Dictionary<Type, MethodInfo> EnumerableToStringMethods = [];

    private const string CollectionToStringName =
        nameof(InternEnumerableToStringHelper<List<int>, int>.CollectionToString);
    private static MethodInfo GetEnumerableToStringMethod(Type underlying)
    {
        if (EnumerableToStringMethods.TryGetValue(underlying, out var method)) return method;

        var type = typeof(InternEnumerableToStringHelper<,>)
            .MakeGenericType(
                underlying,
                underlying.GenericTypeArguments[0]);
        EnumerableToStringMethods.Add(underlying, method = type.GetMethod(CollectionToStringName)!);

        return method;
    }

    private static MethodInfo? GetToStringMethod(Type underlying)
    {
        if (TypeToString.TryGetValue(underlying, out var method)) return method;
        
        if (underlying.GetMethod("ToString", []) is { } typeToString && IsStable(underlying, typeToString.DeclaringType))
        {
            TypeToString.Add(underlying, method = typeToString);
        }
        else if (FindStableType(typeof(IEnumerable<>), underlying) is not null)
        {
            return null;
        }
        else 
            TypeToString.Add(underlying, method = typeof(object).GetMethod("ToString")!);

        return method;
    }
    private static readonly ParameterExpression ContainerParameter = Expression.Parameter(typeof(IContainer), "container");
    private static readonly Dictionary<Type, UnaryExpression> UnderlyingCastExprCache = [];
    private static readonly Dictionary<Type, MemberExpression> UnderlyingPropertyCache = [];

    private static UnaryExpression GetCastIContainerTypeExpression(Type containerType)
    {
        if (!UnderlyingCastExprCache.TryGetValue(containerType, out var castedContainer))
        {
            UnderlyingCastExprCache.Add(containerType, castedContainer = Expression.Convert(ContainerParameter, containerType));
        }

        return castedContainer;
    }

    private static MemberExpression GetValueContainerValueGetter(Type containerType)
    {
        if (!UnderlyingPropertyCache.TryGetValue(containerType, out var valueProperty))
        {
            var castExpr = GetCastIContainerTypeExpression(containerType);
            UnderlyingPropertyCache.Add(containerType, valueProperty = Expression.Property(castExpr, "Value"));
        }

        return valueProperty;
    }
    private static (ParameterExpression, MemberExpression) GetUnderlyingValueExpr(IContainer container)
    {
        var type = container.GetType();
        var valueProperty = GetValueContainerValueGetter(type);

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

    private static MethodCallExpression GetBoxExprToContainerMethod(ParameterExpression lambdaParam)
    {
        return Expression.Call(null, GetBoxExprToContainerMethod(lambdaParam.Type), lambdaParam);
    }

    private static LambdaExpression GetBoxExprToContainerLambda(Type type)
    {
        var param = Expression.Parameter(type);
        return Expression.Lambda(GetBoxExprToContainerMethod(param), param);
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

    private static T ContainerIdentity<T>(Container<T> instance) => instance.Value!;
    private static T Identity<T>(T instance) => instance;
    
    private static readonly Dictionary<Type, MethodInfo> ContainerIdentityMethods = [];
    private static readonly Dictionary<Type, MethodInfo> NonContainerIdentityMethods = [];
    private static readonly MethodInfo ContainerIdentityMethod = ToDelegate(ContainerIdentity<int>)
        .Method.GetGenericMethodDefinition();
    private static readonly MethodInfo IdentityMethod = ToDelegate(Identity<int>)
        .Method.GetGenericMethodDefinition();

    private static MethodInfo MakeIdentity(Type type, bool containerized = true)
    {
        if (containerized && ContainerIdentityMethods.TryGetValue(type, out var method)) return method;
        if (!containerized && NonContainerIdentityMethods.TryGetValue(type, out method)) return method;

        method = containerized
            ? ContainerIdentityMethod.MakeGenericMethod(type)
            : IdentityMethod.MakeGenericMethod(type);
        
        if (containerized) ContainerIdentityMethods.Add(type, method);
        else NonContainerIdentityMethods.Add(type, method);
        
        return method;
    }

    private static readonly Dictionary<(Type, Type), Func<IContainer, IContainer>>
        CompiledConvertIContainerMethods = [];
    
    public static IContainer ConvertToInheritedTypeOfUnderlying(IContainer self, Type target)
    {
        if (CompiledConvertIContainerMethods.TryGetValue((self.UnderlyingType, target), out var method))
        {
            return method(self);
        }
        
        var param = Expression.Parameter(typeof(IContainer));
        
        var containerUnbox = MakeIdentity(self.UnderlyingType);
        var targetIdentity = MakeIdentity(target, containerized: false);

        var castValue = Expression.Call(targetIdentity, Expression.Call(containerUnbox, param));

        var boxMethod = GetBoxExprToContainerMethod(target);

        var finalMethod = Expression.Call(boxMethod, castValue);
        var compiled = Expression.Lambda<Func<IContainer, IContainer>>(finalMethod).Compile();
        
        CompiledConvertIContainerMethods.Add((self.UnderlyingType, target), method = compiled);
        
        return method(self);
    }

    private static MethodCallExpression GetContainerValueLambda(ParameterExpression param)
    {
        var id = MakeIdentity(param.Type);
        var invoke = Expression.Call(id, param);

        return invoke;
    }
    public static MethodCallExpression GetContainerValueLambda(IContainer container)
    {
        var constant = Expression.Constant(container);

        var id = MakeIdentity(container.UnderlyingType);
        var invoke = Expression.Call(id, constant);

        return invoke;
    }

    private static readonly Dictionary<Type, LambdaExpression> UnknownSequenceToContainerSequenceMethod = [];
    private static readonly Dictionary<Type, Type> UnknownSequenceUnderlyingType = [];
    public static (Type, IEnumerable<IContainer>) ConvertUnknownSequenceAsContainerizedSequence(MethodCallExpression sequence)
    {
        var seqOriginalType = sequence.Method.ReturnType;
        if (!UnknownSequenceToContainerSequenceMethod.TryGetValue(seqOriginalType, out var method))
        {
            var sequenceType = seqOriginalType.GetInterfaces().Append(seqOriginalType)
                                   .FirstOrDefault(type => IsGenericStable(typeof(IEnumerable<>), type))
                               ?? throw new InvalidCastException();
        
            var underlyingType = sequenceType.GetGenericArguments().FirstOrDefault()
                                 ?? throw new InvalidProgramException();

            var param = Expression.Parameter(seqOriginalType);
            
            // lambda mapper: T => IContainer
            var mapperMethod = GetBoxExprToContainerLambda(underlyingType).Compile();
            var mapper = Expression.Constant(mapperMethod);
            
            // convert ? to IEnumerable<T>
            // var convertMethod = MakeIdentity(sequenceType, containerized: false);
            // var id = Expression.Call(convertMethod, param);
            // box IEnumerable<T> to IEnumerable<IContainer>
            var select = ConstructSelectMethod(underlyingType, typeof(IContainer));
            
            var convert = Expression.Call(null, select, param, mapper);
            
            // compile convert method
            UnknownSequenceUnderlyingType.Add(seqOriginalType, underlyingType);
            var lambda = Expression.Lambda(convert, param);
            UnknownSequenceToContainerSequenceMethod.Add(seqOriginalType, method = lambda);
        }

        var methodCall = Expression.Invoke(method, sequence);
        var func = Expression.Lambda<Func<IEnumerable<IContainer>>>(methodCall).Compile();
        return (UnknownSequenceUnderlyingType[seqOriginalType], func());
    }

    private static readonly Dictionary<Type, Func<IEnumerable<IContainer>, IContainer>> ContainerSequenceUnwrapMethod = [];
    private static readonly Dictionary<Type, Func<IContainer>> EmptySequenceMethods =  [];
    private static readonly MethodInfo EmptySeqMethod = ToDelegate(Enumerable.Empty<int>).Method.GetGenericMethodDefinition();
    public static IContainer ConvertContainerSequenceAsContainerizedUnknownSequence(Type underlying, List<IContainer> values)
    {
        if (values.Count == 0)
        {
            if (!EmptySequenceMethods.TryGetValue(underlying, out var emptyLambda))
            {
                var emptyMethod = EmptySeqMethod.MakeGenericMethod(underlying);
                var callEmptySeq = Expression.Call(null, emptyMethod);
                var wrapContainer = GetBoxExprToContainerMethod(emptyMethod.ReturnType);
                var callWrap = Expression.Call(null, wrapContainer, callEmptySeq);
                emptyLambda = Expression.Lambda<Func<IContainer>>(callWrap).Compile();
                
                EmptySequenceMethods.Add(underlying, emptyLambda);
            }

            return emptyLambda();
        }
        
        if (!ContainerSequenceUnwrapMethod.TryGetValue(underlying, out var method))
        {
            var containerType = values[0].GetType();
            
            // param: Container<T>
            var param = ContainerParameter;
            // getter: Container<T>.Value.get
            var getter = GetValueContainerValueGetter(containerType);

            // Func<IContainer, T> = getter(caster(param)) 
            var containerGetter = Expression.Lambda(getter, param);
            var containerGetterMethod = Expression.Constant(containerGetter.Compile());
            
            // IEnumerable<IContainer> => IEnumerable<underlying>
            var seqParam = Expression.Parameter(typeof(IEnumerable<IContainer>));
            var seqCaster = ConstructSelectMethod(typeof(IContainer), underlying);
            var callCast = Expression.Call(null, seqCaster, seqParam, containerGetterMethod);

            var box = GetBoxExprToContainerLambda(callCast.Type);
            var callBox = Expression.Invoke(box, callCast);

            var callCastLambda = Expression.Lambda<Func<IEnumerable<IContainer>, IContainer>>(callBox, seqParam); 

            ContainerSequenceUnwrapMethod.Add(underlying, method = callCastLambda.Compile());
        }

        return method(values);
    }
    
    public static Func<T, string> GetExprToStringDelegate<T>(ParameterExpression parameter, MemberExpression member)
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

    public static string GetExprToStringDelegate(IContainer container)
    {
        var (parameter, valueGetter) = GetUnderlyingValueExpr(container);

        var lambda = Expression.Lambda(valueGetter);

        var invoke = Expression.Invoke(lambda);

        var method = GetToStringMethod(container.UnderlyingType);
        var callUnderlyingToString = method is null
            ? Expression.Call(GetEnumerableToStringMethod(container.UnderlyingType), invoke)
            : Expression.Call(invoke, method);

        var toString = Expression.Lambda<Func<IContainer, string>>(callUnderlyingToString, parameter);

        return toString.Compile()(container);
    }

    public static Func<object?> GetBoxUnderlyingValueDelegate(IContainer container)
    {
        var (parameter, value) = GetUnderlyingValueExpr(container);

        var rawFunc = Expression.Lambda<Func<IContainer, object?>>(value, parameter).Compile();

        return () => rawFunc(container);
    }

    private static readonly Dictionary<(Type, Type), Delegate> UnderlyingCastCache = [];
    
    public static async ValueTask<Func<T>> GetValueCastToDelegate<T>(IContainer container)
    {

        var sanitizedContainer = await SanitizeLambdaExpression(container);
        
        var (parameter, value) = GetUnderlyingValueExpr(sanitizedContainer);
        
        if (!value.Type.IsAssignableTo(typeof(T)))
            throw new InvalidCastException($"{value.Type} can't cast to {typeof(T)}");

        if (!UnderlyingCastCache.TryGetValue((value.Type, typeof(T)), out var rawConverter))
        {
            if (value.Type.IsValueType)
            {
                var valueTypeExpr = Expression.Lambda<Func<IContainer, T>>(value, parameter).Compile();

                UnderlyingCastCache.Add((value.Type, typeof(T)), rawConverter = valueTypeExpr);
            }
            else
            {
                var cast = Expression.TypeAs(value, typeof(T));

                var rawFunc = Expression.Lambda<Func<IContainer, T>>(cast, parameter).Compile();

                UnderlyingCastCache.Add((value.Type, typeof(T)), rawConverter = rawFunc);
            }
        }
        var method = (Func<IContainer, T>)rawConverter;
        return () => method(sanitizedContainer);
    }

    public static async ValueTask<IContainer> SanitizeLambdaExpression(IContainer value)
    {
        if (value.UnderlyingType == typeof(LambdaExpression)) {
            var expr = value.As<LambdaExpression>()!;
            var invoke = Expression.Invoke(expr);

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

    public static string GetTypeParamString(Type type) => $"{type.FullName}";
    public static string JoinTypeParams(IEnumerable<string> paramStrings) => string.Join(',', paramStrings);

    public static string GetSignatureOf(IEnumerable<MethodCallExpression> parameters)
    {
        var methodSignatures = parameters.Select(getter => GetTypeParamString(getter.Method.ReturnType));
        return JoinTypeParams(methodSignatures);
    }

    private static List<Type> GetSignatureTypesOf(IEnumerable<MethodCallExpression> parameters)
    {
        return parameters.Select(getter => getter.Method.ReturnType).ToList();
    }

    private static bool IsGenericStable(Type target, Type? source)
    {
        if (source is null) return false;
        
        return source.IsGenericType
               && target.GetGenericTypeDefinition() == source.GetGenericTypeDefinition();
    }
    
    private static bool IsStable(Type target, Type? source)
    {
        if (source is null) return false;
        
        if (target.IsGenericType)
        {
            return source.IsGenericType
                   && target.GetGenericTypeDefinition() == source.GetGenericTypeDefinition();
        }

        return target == source;
    }
    
    private static Type? FindStableType(Type target, Type source)
    {
        if (IsStable(target, source)) return source;

        var interfaces = source.GetInterfaces();
        return interfaces.FirstOrDefault(candidate => IsStable(candidate, target));
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

    private static bool TryBuildGenericMethod(MethodInfo genericMethod, IEnumerable<MethodCallExpression> parameterGetters, [NotNullWhen(true)]out MethodInfo? method)
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

    private static bool MatchSignature(IReadOnlyList<Type> from, IReadOnlyList<Type> to)
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
        methods.Sort((a, _) => a.Item1.IsGenericMethodDefinition ? 1 : -1);
        foreach (var (method, instance) in methods)
        {
            var currentMethod = method;
            if (method.IsGenericMethodDefinition)
            {
                if (!TryBuildGenericMethod(currentMethod, parameterArray, out currentMethod)) continue;
            }

            var methodParamSignatures = currentMethod.GetParameters().Select(param => param.ParameterType).ToList();

            if (!MatchSignature(signature, methodParamSignatures)) continue;
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

        switch (underlying.IsGenericType)
        {
            case true when underlying.GetGenericTypeDefinition() == typeof(ValueTask<>):
            {
                var currentTaskResultType = underlying.GenericTypeArguments[0];
                TypeToCastMethod.Add(underlying, method = AsyncValueTaskCastMethod.MakeGenericMethod(currentTaskResultType));
                break;
            }
            case true when underlying.GetGenericTypeDefinition() == typeof(Task<>):
            {
                var currentTaskResultType = underlying.GenericTypeArguments[0];
                TypeToCastMethod.Add(underlying, method = AsyncTaskCastMethod.MakeGenericMethod(currentTaskResultType));
                break;
            }
            default:
                TypeToCastMethod.Add(underlying, method = ValueTaskCastMethod.MakeGenericMethod(underlying));
                break;
        }

        return method;
    }

    private static ValueTask<IContainer> CastValueTask<T>(T old) => ValueTask.FromResult(IContainer.Of(old));

    private static readonly MethodInfo ValueTaskCastMethod = ToDelegate(CastValueTask<int>)
        .Method.GetGenericMethodDefinition();

    private static async ValueTask<IContainer> CastAsyncValueTask<T>(ValueTask<T> old) => IContainer.Of(await old);

    private static readonly MethodInfo AsyncValueTaskCastMethod = ToDelegate(CastAsyncValueTask<int>)
        .Method.GetGenericMethodDefinition();

    private static async ValueTask<IContainer> CastAsyncTask<T>(Task<T> old) => IContainer.Of(await old);

    private static readonly MethodInfo AsyncTaskCastMethod = ToDelegate(CastAsyncTask<int>)
        .Method.GetGenericMethodDefinition();
}