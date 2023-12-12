using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnalysisScript.Parser.Ast.Basic;

namespace AnalysisScript.Interpreter
{
    public class AsExecutionContext(Action<string> logger)
    {
        public Action<string> Logger { get; } = logger ?? ((_) => { });

        public AsObject CurrentExecuteObject { get; set; }
    }
}
