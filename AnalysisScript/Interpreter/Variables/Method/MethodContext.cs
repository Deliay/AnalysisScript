using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AnalysisScript.Interpreter.Variables.Method
{
    public class MethodContext
    {
        private readonly Dictionary<string, List<(MethodInfo, ConstantExpression?)>> rawMethod = [];
    
        private readonly Dictionary<string, MethodCallExpression> methods = [];

        public IReadOnlyDictionary<string, IEnumerable<(MethodInfo, ConstantExpression?)>> AllRawMethod => rawMethod
            .Select(f => (f.Key, f.Value.AsEnumerable()))
            .ToDictionary();

        public IReadOnlyDictionary<string, MethodCallExpression> AllBuiltMethod => methods;

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

        public MethodContext RegisterStaticFunction(string name, MethodInfo function)
        {
            if (!function.IsStatic)
            {
                throw new NullReferenceException("Can't register instance method to static registry");
            }
            if (!rawMethod.TryGetValue(name, out var methodList))
            {
                rawMethod.Add(name, [(function, null)]);
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
            if (rawMethod.Any(k => k.Key == name && k.Value.Any(m => m.Item1 == registration.method)))
            {
                throw new InvalidOperationException("Duplicated instance registered to this context");
            }
            var constant = Expression.Constant(registration.@this);
            if (!rawMethod.TryGetValue(name, out var methodList))
            {
                rawMethod.Add(name, [(registration.method, constant)]);
            }
            else methodList.Add((registration.method, constant));

            return this;
        }

        private readonly static string ContextParamString = ExprTreeHelper.TypeParamString(typeof(AsExecutionContext));

        private MethodCallExpression BuildMethod(string name, string singedName, IEnumerable<MethodCallExpression> parameters)
        {
            if (!this.rawMethod.TryGetValue(name, out var rawMethods))
                throw new UnknownMethodException(name, singedName);

            var signature = string.Join(',', name, ExprTreeHelper.GetSignatureOf(parameters));

            if (!methods.TryGetValue(signature, out var method))
            {
                var (expr, sign) = ExprTreeHelper.BuildMethod(rawMethods, parameters);
                methods.Add(string.Join(',', name, sign), method = expr);
            }
            return method;
        }

        public MethodCallExpression GetMethod(MethodCallExpression? @this, string name, IEnumerable<MethodCallExpression> paramGetters)
        {
            string[] prefix = (@this is null) switch {
                true => [name],
                _ => [name, ExprTreeHelper.TypeParamString(@this.Method.ReturnType)]
            };
            var paramStrings = prefix.Concat(paramGetters.Select(getter => ExprTreeHelper.TypeParamString(getter.Method.ReturnType)));
            var paramSign = ExprTreeHelper.JoinTypeParams(paramStrings);

            if (!methods.TryGetValue(paramSign, out var method))
                method = BuildMethod(name, paramSign, paramGetters);

            return method;
        }
    }
}
