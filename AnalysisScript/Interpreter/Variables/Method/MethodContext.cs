using System.Linq.Expressions;
using System.Reflection;

namespace AnalysisScript.Interpreter.Variables.Method;

public class MethodContext
{
    private readonly Dictionary<string, List<(MethodInfo, ConstantExpression?)>> _rawMethod = [];
    
    private readonly Dictionary<string, MethodCallExpression> _methods = [];

    public IReadOnlyDictionary<string, IEnumerable<(MethodInfo, ConstantExpression?)>> AllRawMethod => _rawMethod
        .Select(f => (f.Key, f.Value.AsEnumerable()))
        .ToDictionary();

    public IReadOnlyDictionary<string, MethodCallExpression> AllBuiltMethod => _methods;

    private static MethodInfo GetMethodDefinition(MethodInfo method) => method.IsGenericMethod switch
    {
        true => method.GetGenericMethodDefinition(),
        _ => method,
    };

    public static IEnumerable<(string name, MethodInfo method)> ScanClassForStaticMethod<T>() where T : class
    {
        return ScanClassForStaticMethod(typeof(T));
    }
    public static IEnumerable<(string name, MethodInfo method)> ScanClassForStaticMethod(Type type)
    {
        if (type.IsGenericType) throw new InvalidOperationException();
        return type.GetRuntimeMethods()
            .Where(method => (method.Attributes & MethodAttributes.Static) > 0)
            .SelectMany(GetMethodInfoForRegistration);
    }
    public static IEnumerable<(string name, (object @this, MethodInfo method) callInfo)> ScanInstanceForInstanceMethod(object instance)
    {
        var type = instance.GetType();
        return type.GetRuntimeMethods()
            .Where(method => (method.Attributes & MethodAttributes.Static) == 0)
            // don't resolve generic method if who want register
            .Select(method => (method, attributes: method.GetCustomAttributes<AsMethodAttribute>()))
            .Where(info => info.attributes.Any())
            .SelectMany(info => info.attributes.Select(attr => (attr.Name, (instance, info.method))));
    }

    public static IEnumerable<(string name, MethodInfo method)> GetMethodInfoForRegistration(Delegate @delegate)
    {
        return GetMethodInfoForRegistration(@delegate.Method);
    }

    public static IEnumerable<(string name, MethodInfo method)> GetMethodInfoForRegistration(MethodInfo method)
    {
        return GetMethodDefinition(method)
            .GetCustomAttributes<AsMethodAttribute>()
            .Select(attr => (attr.Name, method))
            .ToArray();
    }

    public MethodContext ScanAndRegisterStaticFunction(Type type)
    {
        foreach (var (name, method) in ScanClassForStaticMethod(type))
        {
            RegisterStaticFunction(name, method);
        }
        return this;
    }
        
    public MethodContext ScanAndRegisterInstanceFunction(object @this)
    {
        foreach (var (name, callInfo) in ScanInstanceForInstanceMethod(@this))
        {
            RegisterInstanceFunction(name, callInfo);
        }
        return this;
    }

    public MethodContext RegisterStaticFunction(string name, Delegate function)
    {
        return RegisterStaticFunction(name, function.Method);
    }
    public MethodContext RegisterStaticFunction(string name, MethodInfo function)
    {
        if (!function.IsStatic)
        {
            throw new NullReferenceException("Can't register instance method to static registry");
        }
        if (function.ReturnType == typeof(void))
        {
            throw new InvalidOperationException("Can't register void method");
        }
        if (!_rawMethod.TryGetValue(name, out var methodList))
        {
            _rawMethod.Add(name, [(function, null)]);
        }
        else methodList.Add((function, null));

        return this;
    }

    public MethodContext RegisterInstanceFunction(string name, Delegate @delegate)
    {
        RegisterInstanceFunction(name, (@delegate.Target, @delegate.Method));

        return this;
    }

    public MethodContext RegisterInstanceFunction(string name, (object? @this, MethodInfo method) registration)
    {
        if (registration.method.IsStatic)
        {
            throw new InvalidDataException("Don't register static method to instance registry");
        }
        if (registration.@this is null)
        {
            throw new NullReferenceException("Instance is null when register instance function");
        }
        if (_rawMethod.Any(k => k.Key == name && k.Value.Any(m => m.Item1 == registration.method)))
        {
            throw new InvalidOperationException("Duplicated instance registered to this context");
        }
        if (registration.method.ReturnType == typeof(void))
        {
            throw new InvalidOperationException("Can't register void method");
        }
        var constant = Expression.Constant(registration.@this);
        if (!_rawMethod.TryGetValue(name, out var methodList))
        {
            _rawMethod.Add(name, [(registration.method, constant)]);
        }
        else methodList.Add((registration.method, constant));

        return this;
    }

    private MethodCallExpression BuildMethod(string name, string singedName, IEnumerable<Type> parameters)
    {
        if (!this._rawMethod.TryGetValue(name, out var rawMethods))
            throw new UnknownMethodException(name, singedName);

        var parameterList = parameters.ToList();
        var signature = string.Join(',', name, ExprTreeHelper.GetSignatureOf(parameterList));

        if (!_methods.TryGetValue(signature, out var method))
        {
            var (expr, sign) = ExprTreeHelper.BuildMethod(rawMethods, parameterList);
            _methods.Add(string.Join(',', name, sign), method = expr);
        }
        return method;
    }

    public MethodCallExpression GetMethod(MethodCallExpression? @this, string name, IEnumerable<MethodCallExpression> paramGetters)
    {
        return GetMethod(@this, name, paramGetters.Select(param => param.Method.ReturnType));
    }
    public MethodCallExpression GetMethod(MethodCallExpression? @this, string name, IEnumerable<Type> paramGetters)
    {
        return GetMethod(@this?.Method.ReturnType, name, paramGetters);
    }
    public MethodCallExpression GetMethod(Type? thisType, string name, IEnumerable<Type> paramGetters)
    {
        string[] prefix = (thisType is null) switch {
            true => [name],
            _ => [name, ExprTreeHelper.GetTypeParamString(thisType)]
        };
        var parameterList = paramGetters.ToList();
        var paramStrings = prefix.Concat(parameterList.Select(ExprTreeHelper.GetTypeParamString));
        var paramSign = ExprTreeHelper.JoinTypeParams(paramStrings);

        if (!_methods.TryGetValue(paramSign, out var method))
            method = BuildMethod(name, paramSign, parameterList);

        return method;
    }
}