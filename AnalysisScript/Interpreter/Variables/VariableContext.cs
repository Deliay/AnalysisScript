using AnalysisScript.Interpreter.Variables.Method;
using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AnalysisScript.Interpreter.Variables
{
    public class VariableContext
    {
        private readonly Dictionary<string, IContainer> variables = [];
        private int tempVar = 0;
        public AsIdentity AddTempVar(IContainer value, IToken token)
        {
            var id = new AsIdentity(new Token.Identity($"\"_temp_var_{++tempVar}", token.Pos, token.Line));
            variables.Add(id.Name, value);
            return id;
        }
        public MethodContext Methods { get; }

        public VariableContext()
        {
            Methods = new(this);
        }

        public void PutInitVariable<T>(string name, T value)
        {
            if (variables.ContainsKey(name))
                throw new VariableAlreadyExistsException($"Initialize variable {name} already exists");

            variables.Add(name, IContainer.Of(value));
        }

        public void PutVariable<T>(AsIdentity id, T value)
        {
            if (variables.ContainsKey(id.Name))
                throw new VariableAlreadyExistsException(id);

            variables.Add(id.Name, IContainer.Of(value));
        }

        public void PutVariableContainer(AsIdentity id, IContainer value)
        {
            if (variables.ContainsKey(id.Name))
                throw new VariableAlreadyExistsException(id);

            variables.Add(id.Name, value);
        }


        public bool HasVariable(AsIdentity id) => variables.ContainsKey(id.Name);

        public T? GetVariable<T>(AsIdentity id)
        {
            if (!variables.TryGetValue(id.Name, out var container))
                throw new UnknownVariableException(id);

            return container.As<T>();
        }
        public IContainer GetVariableContainer(AsIdentity id)
        {
            if (!variables.TryGetValue(id.Name, out var container))
                throw new UnknownVariableException(id);

            return container;
        }


        public object? __Boxed_GetVariable(AsIdentity id)
        {
            if (!variables.TryGetValue(id.Name, out var container))
                throw new UnknownVariableException(id);

            return container.BoxedUnderlyingValue();
        }

        public void __Unsafe_UnBoxed_PutVariable(string name, object value)
        {
            variables.Add(name, ExprTreeHelper.UnboxToContainer(value)(value));
        }

        public AsIdentity Storage(AsObject @object)
        {
            if (@object is AsString str) return AddTempVar(IContainer.Of(Interpolation(str)), str.LexicalToken);
            else if (@object is AsInteger integer) return AddTempVar(IContainer.Of(integer.Value), integer.LexicalToken);
            else if (@object is AsNumber num) return AddTempVar(IContainer.Of(num.Real), num.LexicalToken);
            else if (@object is AsIdentity id) return id;
            throw new UnknownValueObjectException(@object);
        }

        public object ValueOf(AsObject @object)
        {
            if (@object is AsString str) return Interpolation(str);
            else if (@object is AsNumber num) return num.Real;
            else if (@object is AsInteger integer) return integer.Value;
            else if (@object is AsIdentity id) return __Boxed_GetVariable(id);
            throw new UnknownValueObjectException(@object);
        }

        public MethodCallExpression LambdaValueOf(AsObject @object)
        {
            if (@object is AsString str) return ExprTreeHelper.GetConstantValueLambda(Interpolation(str));
            else if (@object is AsInteger integer) return ExprTreeHelper.GetConstantValueLambda(integer.Value);
            else if (@object is AsNumber num) return ExprTreeHelper.GetConstantValueLambda(num.Real);
            else if (@object is AsIdentity id) return ExprTreeHelper.GetContainerValueLambda(GetVariableContainer(id));
            throw new UnknownValueObjectException(@object);
        }

        private IEnumerable<MethodCallExpression> args(IEnumerable<AsObject> arguments, MethodCallExpression @this, AsExecutionContext ctx)
        {
            yield return ExprTreeHelper.GetConstantValueLambda(ctx);
            yield return @this;
            foreach (var item in arguments)
            {
                var expr = LambdaValueOf(item);
                yield return expr;
            }
        }

        public Expression<Func<ValueTask<IContainer>>> GetMethodCallLambda(MethodCallExpression @this, string name, List<AsObject> methodParams, AsExecutionContext ctx)
        {
            var exprValues = args(methodParams, @this, ctx);
            var method = Methods.GetMethod(@this, name, exprValues);

            method = method.Update(method.Object, exprValues.Cast<Expression>());
            var retValueType = ExprTreeHelper.GetTypeToCastMethod(method.Method.ReturnType);
            method = Expression.Call(retValueType, method);

            return Expression.Lambda<Func<ValueTask<IContainer>>>(method);
        }

        public string Interpolation(AsString str)
        {
            var currentStr = str.RawContent;
            List<string> slices = [];
            int pos = 0;
            int left = currentStr.IndexOf("${");
            int right = currentStr.IndexOf('}');

            while (left > -1 && right > -1)
            {
                var varName = currentStr[(left + 2)..right];
                if (!variables.TryGetValue(varName, out var value))
                    throw new UnknownVariableException(str.LexicalToken);

                slices.Add(currentStr[pos..left]);
                slices.Add(value.UnerlyingToString() ?? "(null)");
                pos = right + 1;
                left = currentStr.IndexOf("${", pos);
                if (left == -1) break;
                right = currentStr.IndexOf('}', left);
            }
            slices.Add(currentStr[pos..]);

            return string.Join("", slices);
        }
    }
}
