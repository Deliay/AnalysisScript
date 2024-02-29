using System.Linq.Expressions;
using System.Reflection;
using AnalysisScript.Interpreter.Variables;
using AnalysisScript.Interpreter.Variables.Method;
using AnalysisScript.Lexical;
using AnalysisScript.Parser;
using AnalysisScript.Parser.Ast;
using AnalysisScript.Parser.Ast.Basic;
using AnalysisScript.Parser.Ast.Command;
using AnalysisScript.Parser.Ast.Operator;

namespace AnalysisScript.Interpreter;

public class CodeStaticAnalyzer(VariableContext previewContext)
{
    public enum ErrorTypes
    {
        Lexical,
        Parser,
        Runtime,
    }

    private static (List<IToken> result, Exception? e) PreviewLexicalAnalyze(string code)
    {
        try
        {
            var tokens = LexicalAnalyzer.Analyze(code).ToList();
            return (tokens, null);
        }
        catch (Exception e)
        {
            return ([], e);
        }
    }

    private static (AsAnalysis? ast, Exception? e) PreviewParse(IEnumerable<IToken> token)
    {
        try
        {
            var ast = ScriptParser.Parse(token);
            return (ast, null);
        }
        catch (Exception e)
        {
            return (null!, e);
        }
    }

    public IReadOnlyDictionary<AsCall, MethodInfo> CallsMethod => _callsMethod;
    private readonly Dictionary<AsCall, MethodInfo> _callsMethod = [];
    public IReadOnlyDictionary<AsPipe, MethodInfo> PipesMethod => _pipesMethod;
    private readonly Dictionary<AsPipe, MethodInfo> _pipesMethod = [];
    private readonly Dictionary<AsIdentity, Type> _varMap = [];
    public IReadOnlyDictionary<AsIdentity, Type> VariableTypes => _varMap;
    private List<IToken> _lexicalTokens = [];
    private IReadOnlyList<IToken> LexicalTokens => _lexicalTokens;
    public AsAnalysis? SyntaxTree { get; private set; }
    public Type? ReturnType { get; private set; }
    private static Type UnwrapAsyncType(Type type)
    {
        if (ExprTreeHelper.IsGenericStable(typeof(Task<>), type)
            || ExprTreeHelper.IsGenericStable(typeof(ValueTask<>), type))
        {
            return type.GetGenericArguments()[0];
        }

        return type;
    }

    private IEnumerable<object?> ResolveArgs(IEnumerable<AsObject> args, Type thisType)
    {
        yield return null;
        yield return ExprTreeHelper.MakeEmptyEnumerable(thisType);
        foreach (var asObject in args)
        {
            yield return asObject switch
            {
                AsString str => str.RawContent,
                AsInteger i => i.Value,
                AsNumber n => n.Real,
                _ => ExprTreeHelper.MakeEmptyEnumerable(previewContext.TypeOf(asObject, id => _varMap[id])),
            };
        }
    }
    
    private Type UnwrapExprReturnValue(AsExecutionContext ctx, MethodCallExpression callExpr, IEnumerable<AsObject> args)
    {
        var method = callExpr.Method;
        if (method.ReturnType != typeof(LambdaExpression))
        {
            return method.ReturnType;
        }

        var paramList = callExpr.Arguments.Select(t => t as ParameterExpression ?? Expression.Parameter(t.Type)).ToList();
        var @delegate = Expression.Lambda(callExpr, paramList).Compile();
        var realArgs = ResolveArgs(args, paramList[1].Type);

        try
        {
            var objects = realArgs.ToArray();
            var ret = @delegate.DynamicInvoke(objects);
            if (ret is LambdaExpression lambdaExpression)
            {
                return lambdaExpression.ReturnType;
            }
        }
        catch (Exception e)
        {
            throw new AsRuntimeException(ctx, e, AsRuntimeError.NotAnalyzable);
        }
        return method.ReturnType;
    }
    
    private (int, Exception? e) PreviewCallInterpreter(VariableContext vars, AsExecutionContext ctx, AsCall asCall)
    {
        if (!vars.Methods.HasMethod(asCall.Method.Name))
        {
            return (asCall.LexicalToken.Line, new AsRuntimeException(ctx,
                new UnknownMethodException(asCall.Method.Name, ""),
                AsRuntimeError.NoMatchedMethod));
        }

        try
        {
            var method = vars.BuildMethodCallExpr(null, asCall.Method.Name, asCall.Args, id => _varMap[id]);
            _callsMethod.Add(asCall, method.Method);
            return (0, null);
        }
        catch (NoMethodMatchedException e)
        {
            return (asCall.LexicalToken.Line, new AsRuntimeException(ctx, e, AsRuntimeError.NoMatchedMethod));
        }
        catch (Exception e)
        {
            return (asCall.LexicalToken.Line, new AsRuntimeException(ctx, e));
        }
    }
    
    private (Type returnType, Exception? e) PreviewPipeRunning(AsExecutionContext ctx, VariableContext vars, Type refType, AsPipe asPipe)
    {
        if (!vars.Methods.HasMethod(asPipe.FunctionName.Name))
        {
            return (null!, new AsRuntimeException(ctx,
                new UnknownMethodException(asPipe.FunctionName.Name, ""),
                AsRuntimeError.NoMatchedMethod));
        }

        try
        {
            var safeType = UnwrapAsyncType(refType);
            var previousValue = asPipe.DontSpreadArg ? null : safeType;
            if (asPipe.ForEach)
            {
                var enumerableType = ExprTreeHelper.FindStableType(typeof(IEnumerable<>), refType)
                    ?? ExprTreeHelper.FindStableType(typeof(IAsyncEnumerable<>), refType);

                if (enumerableType is null)
                    return (null!,
                        new AsRuntimeException(ctx, null!, AsRuntimeError.NotEnumerable));
                
                var rawType = UnwrapAsyncType(enumerableType.GetGenericArguments()[0]);
                var callExpr = vars.BuildMethodCallExpr(asPipe.DontSpreadArg ? null : rawType, asPipe.FunctionName.Name, asPipe.Arguments, 
                    id => _varMap[id], () => rawType);
                var returnType = UnwrapExprReturnValue(ctx, callExpr, asPipe.Arguments);
                
                _pipesMethod.Add(asPipe, callExpr.Method);

                return (typeof(IAsyncEnumerable<>).MakeGenericType(UnwrapAsyncType(returnType)), null);
            }
            else
            {
                var callExpr = vars.BuildMethodCallExpr(previousValue, asPipe.FunctionName.Name, asPipe.Arguments, id => _varMap[id], null);
                var returnType = UnwrapExprReturnValue(ctx, callExpr, asPipe.Arguments);
                
                _pipesMethod.Add(asPipe, callExpr.Method);
                return (UnwrapAsyncType(returnType), null!);
            }
        }
        catch (UnknownMethodException e)
        {
            return (null!, new AsRuntimeException(ctx, e, AsRuntimeError.NoMatchedMethod));
        }
        catch (NoMethodMatchedException e)
        {
            return (null!, new AsRuntimeException(ctx, e, AsRuntimeError.NoMatchedMethod));
        }
        catch (Exception e)
        {
            return (null!, new AsRuntimeException(ctx, e));
        }
    }

    private class Void;
    
    private IEnumerable<(int, Exception e)> PreviewInterpreter<T>(VariableContext vars, AsAnalysis? ast)
    {
        if (ast is null || ast.Commands.Count == 0)
        {
            yield break;
        }

        var ctx = new AsExecutionContext(vars, _ => { });
        foreach (var command in ast.Commands)
        {
            ctx.CurrentExecuteObject = command;
            switch (command.Type)
            {
                case CommandType.Param when command is AsParam asParam:
                {
                    if (!vars.HasVariable(asParam.Variable))
                    {
                        yield return (asParam.LexicalToken.Line, new AsRuntimeException(ctx,
                            new UnknownVariableException(asParam.Variable),
                            AsRuntimeError.VariableNotInitialized));
                        yield break;
                    }

                    _varMap.TryAdd(asParam.Variable, vars.GetVariableContainer(asParam.Variable).UnderlyingType);
                    break;
                }
                case CommandType.Return when command is AsReturn asReturn:
                {
                    if (!vars.HasVariable(asReturn.Variable))
                    {
                        yield return (asReturn.LexicalToken.Line, new AsRuntimeException(ctx,
                            new UnknownVariableException(asReturn.Variable), AsRuntimeError.VariableNotInitialized));
                        yield break;
                    }

                    var returnVariableType = vars.TypeOf(asReturn.Variable, id => _varMap[id]);
                    if (typeof(T) == typeof(Void) || returnVariableType != typeof(T))
                    {
                        yield return (asReturn.LexicalToken.Line,
                            new AsRuntimeException(ctx, null!, AsRuntimeError.InvalidReturnType));
                        yield break;
                    }
                    
                    ReturnType = returnVariableType;

                    break;
                }
                case CommandType.Call when command is AsCall asCall:
                {
                    var (line, e) = PreviewCallInterpreter(vars, ctx, asCall);
                    if (e is not null) yield return (line, e);
                    break;
                }
                case CommandType.Let when command is AsLet asLet:
                {
                    if (vars.HasVariable(asLet.Name) || _varMap.ContainsKey(asLet.Name))
                    {
                        yield return (asLet.LexicalToken.Line,
                            new AsRuntimeException(ctx, null!, AsRuntimeError.VariableAlreadyExist));
                    }

                    _varMap[AsIdentity.Reference] = vars.TypeOf(asLet.Arg, id => _varMap[id]);

                    foreach (var asPipe in asLet.Pipes)
                    {
                        ctx.CurrentExecuteObject = asPipe;
                        var (returnType, e) = PreviewPipeRunning(ctx, vars, _varMap[AsIdentity.Reference], asPipe);
                        _varMap[AsIdentity.Reference] = returnType;
                        _varMap[vars.GetNextTempVar(asPipe.LexicalToken)] = returnType;
                        
                        if (e is null) continue;

                        yield return (asPipe.LexicalToken.Line, e);
                        yield break;
                    }

                    _varMap[asLet.Name] = _varMap[AsIdentity.Reference];
                    _varMap.Remove(AsIdentity.Reference);
                    break;
                }
            }
        }
    }

    public IEnumerable<(int, ErrorTypes, Exception)> PreviewErrors(string code)
        => PreviewErrors<Void>(code);

    public IEnumerable<(int, ErrorTypes, Exception)> PreviewErrors<TReturn>(string code)
    {
        _varMap.Clear();
        _pipesMethod.Clear();
        _callsMethod.Clear();
        ReturnType = null;
        var (result, lexicalException) = PreviewLexicalAnalyze(code);
        if (lexicalException is not null)
        {
            if (lexicalException is not InvalidTokenException invalidTokenException)
                throw lexicalException;
                
            yield return (invalidTokenException.Line, ErrorTypes.Lexical, lexicalException);
            yield break;
        }

        this._lexicalTokens = result;
        var (ast, parseException) = PreviewParse(result);
        if (parseException is not null)
        {
            if (parseException is not InvalidGrammarException invalidGrammarException)
                throw parseException;

            yield return (invalidGrammarException.Actual.Line, ErrorTypes.Parser, parseException);
            yield break;
        }

        this.SyntaxTree = ast;
        foreach (var (line, ex) in PreviewInterpreter<TReturn>(previewContext, ast))
        {
            yield return (line, ErrorTypes.Runtime, ex);
        }
    }

}