using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Parser.Ast
{
    public enum CommandType
    {
        Call,
        Let,
        Comment,
        Param,
        Return,
    }
}
