using AnalysisScript.Interpreter.Variables.Method;
using AnalysisScript.Parser.Ast.Basic;
using AnalysisScript.Parser.Ast.Operator;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Interpreter.Variables
{
    public class VariableContext
    {
        private readonly Dictionary<string, IContainer> variables = [];
        private int tempVar = 0;
        public AsIdentity AddTempVar(IContainer value, AsPipe pipe)
        {
            var id = new AsIdentity(new Lexical.Token.Identity($"\"_temp_var_{tempVar++}", pipe.LexicalToken.Pos));
            variables.Add(id.Name, value);
            return id;
        }
        public MethodContext Methods { get; }

        public VariableContext()
        {
            Methods = new(this);
        }

        public void PutVariable<T>(AsIdentity id, T value)
        {
            if (variables.ContainsKey(id.Name))
                throw new VariableAlreadyExistsException(id);

            variables.Add(id.Name, IContainer.Of(value));
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

        public object ValueOf(AsObject @object)
        {
            if (@object is AsString str) return Interpolation(str);
            else if (@object is AsNumber num) return num.Real;
            else if (@object is AsIdentity id) return __Boxed_GetVariable(id);
            throw new UnknownValueObjectException(@object);
        }

        public MethodCallExpression LambdaValueOf(AsObject @object)
        {
            if (@object is AsString str) return ExprTreeHelper.GetConstantValueLambda(Interpolation(str));
            else if (@object is AsNumber num) return ExprTreeHelper.GetConstantValueLambda(num.Real);
            else if (@object is AsIdentity id) return ExprTreeHelper.GetContainerValueLambda(GetVariableContainer(id));
            throw new UnknownValueObjectException(@object);
        }

        public Expression<Func<ValueTask<IContainer>>> GetMethodCallLambda(MethodCallExpression @this, string name, List<AsObject> methodParams)
        {
            var exprValues = methodParams.Select(LambdaValueOf);
            var method = Methods.GetMethod(@this, name, exprValues);
            var init = new[] { @this };

            method = method.Update(method.Object, init.Concat(exprValues.Cast<Expression>()));
            method = Expression.Call(ExprTreeHelper.GetTypeToCastMethod(method.Method.ReturnType), method);

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
