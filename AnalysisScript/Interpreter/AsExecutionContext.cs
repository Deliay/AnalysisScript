using AnalysisScript.Interpreter.Variables;
using AnalysisScript.Parser.Ast.Basic;

namespace AnalysisScript.Interpreter;

public class AsExecutionContext(VariableContext ctx, Action<string> logger, CancellationToken cancellationToken = default)
{
    public AsExecutionContext(Action<string> logger, CancellationToken cancellationToken = default) : this(null!, logger, cancellationToken)
    {
        
    }
    public AsExecutionContext(CancellationToken cancellationToken = default) : this((_) => { }, cancellationToken)
    {
    }

    public Action<string> Logger { get; } = logger ?? ((_) => { });

    public AsObject? CurrentExecuteObject { get; internal set; }

    public VariableContext VariableContext { get; internal set; } = ctx;

    public CancellationToken CancelToken => cancellationToken;
}