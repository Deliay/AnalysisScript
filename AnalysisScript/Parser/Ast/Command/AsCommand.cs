﻿using AnalysisScript.Lexical;
using AnalysisScript.Parser.Ast.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalysisScript.Parser.Ast.Command
{
    public abstract class AsCommand(IToken lexical, CommandType type) : AsObject(lexical)
    {
        public CommandType Type => type;
    }
    public abstract class AsCommand<T>(T lexical, CommandType type)
        : AsCommand(lexical, type)
        where T : IToken
    {
        public new T LexicalToken => lexical;
    }
}
