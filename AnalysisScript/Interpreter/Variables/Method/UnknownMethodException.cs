using AnalysisScript.Parser.Ast.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Interpreter.Variables.Method
{
    public class UnknownMethodException(string methodName, string signature) : Exception($"Unknown method: {methodName}({signature})")
    {
    }
}
