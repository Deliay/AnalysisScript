using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AnalysisScript.Interpreter.Variables
{
    public interface IContainer
    {
        public Type UnderlyingType { get; }

        private readonly static MethodInfo _OfMethodInfo = typeof(IContainer).GetMethod(nameof(As))!;
        public static MethodInfo OfMethodInfo(Type type) => _AsMethodInfo.MakeGenericMethod(type);

        public static IContainer Of<T>(T value) => new Container<T>(value);

        private readonly static MethodInfo _AsMethodInfo = typeof(IContainer).GetMethod(nameof(As))!;
        public MethodInfo AsMethodInfo() => _AsMethodInfo.MakeGenericMethod(UnderlyingType);

        public T? As<T>() => ((Container<T>)this).Value;


        public string? UnerlyingToString() => ExprTreeHelper.ExprToString(this)();

        public object? BoxedUnderlyingValue() => ExprTreeHelper.BoxUnderlyingValue(this)();
    }

    public struct Container<T>(T? value) : IContainer
    {
        public readonly Type UnderlyingType => typeof(T);
        public readonly T? Value => value;

    }
}
