using System.Collections;

namespace AnalysisScript.Library;

public static class Assert
{
    public static void Count(int atLeast, object[] @params)
    {
        if (@params.Length < atLeast)
            throw new ArgumentException($"Require at least {atLeast} param, but only passed {@params.Length}");
    }

    public static void Is<T>(object src, out T target)
    {
        if (src is T convert)
        {
            target = convert;
            return;
        }

        throw new InvalidOperationException($"Require {typeof(T).Name} but passed {src.GetType().Name}");
    }

    public static void IsSeq(object src, out IEnumerable target) => Is(src, out target);
    public static void IsSeq<T>(object src, out IEnumerable<T> target) => Is(src, out target);
}
