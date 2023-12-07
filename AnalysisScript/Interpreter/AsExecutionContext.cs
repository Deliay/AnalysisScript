using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Interpreter
{
    public class AsExecutionContext(Action<string> logger)
    {
        public Action<string> Logger { get; } = logger ?? ((_) => { });
    }
}
