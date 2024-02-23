using AnalysisScript.Interpreter.Variables.Method;
using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast.Basic;
using System.Linq.Expressions;
using System.Reflection;

namespace AnalysisScript.Interpreter.Variables;

public class VariableContext(MethodContext methods)
{
    public IEnumerable<KeyValuePair<AsIdentity, IContainer>> AllVariables => _variables;
    private readonly VariableMap _variables = [];
    private int _tempVar = 0;
    public AsIdentity AddTempVar(IContainer value, IToken token)
    {
        var id = new AsIdentity(new Token.Identity($"\"_temp_var_{++_tempVar}", token.Pos, token.Line));
        _variables.Add(id, value);
        return id;
    }
    public MethodContext Methods { get; } = methods;

    public VariableContext() : this(new MethodContext()) { }
    
    public VariableContext AddInitializeVariable<T>(string name, T value)
    {
        PutInitVariable(new AsIdentity(new Lexical.Token.Identity(name, 0, 0)), value);
        return this;
    }
    
    public void PutInitVariable<T>(AsIdentity id, T value)
    {
        if (_variables.ContainsKey(id))
            throw new VariableAlreadyExistsException($"Initialize variable {id} already exists");

        _variables.Add(id, IContainer.Of(value));
    }

    public void PutVariable<T>(AsIdentity id, T value)
    {
        if (_variables.ContainsKey(id))
            throw new VariableAlreadyExistsException(id);

        _variables.Add(id, IContainer.Of(value));
    }

    public void PutVariableContainer(AsIdentity id, IContainer value)
    {
        if (_variables.ContainsKey(id))
            throw new VariableAlreadyExistsException(id);

        _variables.Add(id, value);
    }

    public void AddOrUpdateVariable(AsIdentity id, IContainer value)
    {
        if (_variables.ContainsKey(id))
            _variables.UpdateReference(id, value);
        else _variables.Add(id, value);
    }

    public void Clear()
    {
        _variables.Clear();
    }

    public bool HasVariable(AsIdentity id) => _variables.ContainsKey(id);

    public void UpdateVariable(AsIdentity id)
    {
        _variables.Update(id);
    }

    public T? GetVariable<T>(AsIdentity id)
    {
        if (!_variables.TryGetValue(id, out var container))
            throw new UnknownVariableException(id);

        return container.As<T>();
    }
    public IContainer GetVariableContainer(AsIdentity id)
    {
        if (!_variables.TryGetValue(id, out var container))
            throw new UnknownVariableException(id);

        return container;
    }


    public object? __Boxed_GetVariable(AsIdentity id)
    {
        if (!_variables.TryGetValue(id, out var container))
            throw new UnknownVariableException(id);

        return container.BoxedUnderlyingValue();
    }

    public AsIdentity Storage(AsObject @object)
    {
        return @object switch
        {
            AsString str => AddTempVar(IContainer.Of(Interpolation(str)), str.LexicalToken),
            AsInteger integer => AddTempVar(IContainer.Of(integer.Value), integer.LexicalToken),
            AsNumber num => AddTempVar(IContainer.Of(num.Real), num.LexicalToken),
            AsArray arr => AddTempVar(BuildArray(arr), arr.LexicalToken),
            AsIdentity id => id,
            _ => throw new UnknownValueObjectException(@object)
        };
    }

    private Type PeekArrayType(AsArray array, Func<Type>? referenceType = default)
    {
        return array.Items.Select(item => TypeOf(item, referenceType)).FirstOrDefault() ?? throw new InvalidDataException();
    }
    private IContainer BuildArray(AsArray array)
    {
        Type? basicType = null;
        var exprList = Enumerator().ToList();
        
        return ExprTreeHelper.GetConstantValueLambda(basicType!, exprList);

        IEnumerable<MethodCallExpression> Enumerator()
        {
            foreach (var item in array.Items)
            {
                var container = LambdaValueOf(item);
                
                if (basicType is null)
                {
                    basicType = container.Method.ReturnType;
                }
                else
                {
                    if (container.Method.ReturnType != basicType)
                    {
                        throw new InvalidArrayType(array.LexicalToken, basicType, container.Method.ReturnType);
                    }
                }

                yield return container;
            }
        }
    }
    
    public Type TypeOf(AsObject @object, Func<Type>? referenceType = default)
    {
        return @object switch
        {
            AsString => typeof(string),
            AsInteger integer => typeof(int),
            AsNumber num => typeof(double),
            AsArray arr => PeekArrayType(arr, referenceType),
            AsIdentity id => referenceType is not null && id.Name == "&"
                ? referenceType()
                : GetVariableContainer(id).UnderlyingType,
            _ => throw new UnknownValueObjectException(@object)
        };
    }
    public MethodCallExpression LambdaValueOf(AsObject @object)
    {
        return @object switch
        {
            AsString str => ExprTreeHelper.GetConstantValueLambda(Interpolation(str)),
            AsInteger integer => ExprTreeHelper.GetConstantValueLambda(integer.Value),
            AsNumber num => ExprTreeHelper.GetConstantValueLambda(num.Real),
            AsArray arr => ExprTreeHelper.GetConstantValueLambda(BuildArray(arr)),
            AsIdentity id => ExprTreeHelper.GetContainerValueLambda(GetVariableContainer(id)),
            _ => throw new UnknownValueObjectException(@object)
        };
    }

    private IEnumerable<MethodCallExpression> Args(IEnumerable<AsObject> arguments, MethodCallExpression? @this, AsExecutionContext ctx)
    {
        yield return ExprTreeHelper.GetConstantValueLambda(ctx);
        if (@this is not null) yield return @this;
        foreach (var item in arguments)
        {
            var expr = LambdaValueOf(item);
            yield return expr;
        }
    }

    private IEnumerable<Type> ArgTypes(IEnumerable<AsObject> arguments, Type? thisType, Func<Type>? referenceType = default)
    {
        yield return typeof(AsExecutionContext);
        if (thisType is not null) yield return thisType;
        foreach (var arg in arguments)
        {
            yield return TypeOf(arg, referenceType);
        }
    }

    public MethodInfo BuildMethod(
        Type? thisType, string name, IEnumerable<AsObject> methodParams, Func<Type>? referenceType = default)
    {
        return Methods.GetMethod(thisType, name, ArgTypes(methodParams, thisType, referenceType)).Method;
    }

    public Expression<Func<ValueTask<IContainer>>> GetMethodCallLambda(
        MethodCallExpression? @this, string name, IEnumerable<AsObject> methodParams, AsExecutionContext ctx)
    {
        var exprValues = Args(methodParams, @this, ctx);
        var exprList = exprValues.ToList();
        var methodExpr = Methods.GetMethod(@this, name, exprList);

        methodExpr = methodExpr.Update(methodExpr.Object, exprList);
        var retValueType = ExprTreeHelper.GetTypeToCastMethod(methodExpr.Method.ReturnType);
        methodExpr = Expression.Call(retValueType, methodExpr);

        return Expression.Lambda<Func<ValueTask<IContainer>>>(methodExpr);
    }

    public Expression<Func<ValueTask<IContainer>>> GetMethodCallLambda(
        string name, IEnumerable<AsObject> methodParams, AsExecutionContext ctx)
    {
        return GetMethodCallLambda(null!, name, methodParams, ctx);
    }

    public string Interpolation(AsString str)
    {
        return Interpolation(str.RawContent, str.LexicalToken);
    }
    public string Interpolation(string currentStr, IToken token)
    {
        List<string> slices = [];
        var pos = 0;
        var left = currentStr.IndexOf("${");
        var right = currentStr.IndexOf('}');

        while (left > -1 && right > -1)
        {
            var varName = currentStr[(left + 2)..right];
            if (!_variables.TryGetValue(varName, out var value))
                throw new UnknownVariableException(varName, token);

            slices.Add(currentStr[pos..left]);
            slices.Add(value.UnderlyingToString() ?? "(null)");
            pos = right + 1;
            left = currentStr.IndexOf("${", pos);
            if (left == -1) break;
            right = currentStr.IndexOf('}', left);
        }
        slices.Add(currentStr[pos..]);

        return string.Join("", slices);
    }
}