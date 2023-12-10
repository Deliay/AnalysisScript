using AnalysisScript.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Library
{
    public static class BasicFunctionV2
    {

        public static R Select<T, R>(AsExecutionContext ctx, T @this, string propertyName)
        {
            var param = Expression.Parameter(@this.GetType());
            var property = Expression.Property(param, propertyName);

            return Expression.Lambda<Func<T, R>>(property, param).Compile()(@this);
        }
        public static AsInterpreter RegisterBasicFunctionsV2(this AsInterpreter interpreter)
        {
            interpreter.RegisterFunction("select", typeof(BasicFunctionV2).GetMethod(nameof(Select)));
            return interpreter;
        }
    }
}
