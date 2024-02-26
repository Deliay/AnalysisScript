using AnalysisScript.Lexical;

namespace AnalysisScript.Interpreter;

public class AsRuntimeException(AsExecutionContext ctx, Exception? inner, AsRuntimeError runtimeError = AsRuntimeError.Unknown)
    : Exception($"{runtimeError} at line {ctx.CurrentExecuteObject?.LexicalToken.Line}" +
                $": {ctx.CurrentExecuteObject?.LexicalToken.ToReadableString()}\n{inner?.Message}", inner)
{
    public AsRuntimeError RuntimeError => runtimeError;
    public AsExecutionContext Context => ctx;
}