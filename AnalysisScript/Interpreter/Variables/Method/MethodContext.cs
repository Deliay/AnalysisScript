using AnalysisScript.Library;
using AnalysisScript.Parser.Ast.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AnalysisScript.Interpreter.Variables.Method
{
    public class MethodContext(VariableContext variableContext)
    {
        private readonly Dictionary<string, MethodInfo> rawMethod = [];
    {
        private readonly Dictionary<string, MethodCallExpression> methods = [];

        public void RegisterFunction(string name, MethodCallExpression function)
        {
            rawMethod.Add(name, function);
        }

        public void RegisterFunction(string name, MethodInfo function)
        {
        }

        private readonly static string ContextParamString = ExprTreeHelper.TypeParamString(typeof(AsExecutionContext));

        private MethodCallExpression BuildMethod(string name, string singedName)
        {
            if (!methods.TryGetValue(name, out var method))
                throw new UnknownMethodException(name, singedName);

            var (expr, sign) = ExprTreeHelper.BuildMethod(function);
            rawMethod.Add($"{name}_{sign}", expr);
        }

        public MethodCallExpression GetMethod(MethodCallExpression @this, string name, IEnumerable<MethodCallExpression> paramGetters)
        {
            var prefix = new string[2] {
                ContextParamString,
                ExprTreeHelper.TypeParamString(@this.Method.ReturnType),
            };
            var paramStrings = prefix.Concat(paramGetters.Select(getter => ExprTreeHelper.TypeParamString(getter.Method.ReturnType)));
            var paramSign = ExprTreeHelper.JoinTypeParams(paramStrings);

            if (!methods.TryGetValue(paramSign, out var method))
                throw new UnknownMethodException(name, paramSign);

            return method;
        }
    }
}
