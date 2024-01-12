namespace AnalysisScript.Interpreter;

public class AsRuntimeException(AsExecutionContext context, Exception inner)
    : Exception($"Execution failed at line {context.CurrentExecuteObject?.LexicalToken.Line}: {context.CurrentExecuteObject}\n{inner.Message}", inner)
{
    public AsExecutionContext Context => context;
}