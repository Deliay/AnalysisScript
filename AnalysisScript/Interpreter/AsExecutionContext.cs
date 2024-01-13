﻿using AnalysisScript.Interpreter.Variables;
using AnalysisScript.Parser.Ast.Basic;

namespace AnalysisScript.Interpreter;

public class AsExecutionContext(Action<string> logger, CancellationToken cancellationToken = default)
{
    public AsExecutionContext(CancellationToken cancellationToken = default) : this((_) => { }, cancellationToken)
    {
    }

    public Action<string> Logger { get; } = logger ?? ((_) => { });

    public AsObject? CurrentExecuteObject { get; internal set; }

    public VariableContext VariableContext { get; internal set; }

    public CancellationToken CancelToken => cancellationToken;
}