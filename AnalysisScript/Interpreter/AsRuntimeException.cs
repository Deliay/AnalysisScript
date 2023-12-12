using AnalysisScript.Parser.Ast.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Interpreter
{
    public class AsRuntimeException(AsExecutionContext context, Exception inner)
        : Exception($"Execution failed at line {context.CurrentExecuteObject.LexicalToken.Line}: {context.CurrentExecuteObject}\n{inner.Message}", inner)
    {
        public AsExecutionContext Context => context;
    }
}
