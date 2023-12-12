using AnalysisScript.Parser.Ast.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Interpreter
{
    public class AsRuntimeException(AsExecutionContext Context, Exception Inner)
        : Exception($"{Inner.Message} \n Execution failed at line {Context.CurrentExecuteObject.LexicalToken.Line}: {Context.CurrentExecuteObject}")
    {
    }
}
