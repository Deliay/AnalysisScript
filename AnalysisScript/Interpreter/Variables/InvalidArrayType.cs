using System.Reflection;
using AnalysisScript.Lexical;

namespace AnalysisScript.Interpreter.Variables;

public class InvalidArrayType(IToken lexical, MemberInfo required, MemberInfo actual)
    : ArrayTypeMismatchException($"Invalid array type at {lexical.Pos}, line {lexical.Line}, require {required.Name}, actual {actual.Name}")
{
    
}