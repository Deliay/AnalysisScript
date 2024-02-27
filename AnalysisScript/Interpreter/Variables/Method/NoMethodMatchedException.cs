using System.Linq.Expressions;
using System.Reflection;

namespace AnalysisScript.Interpreter.Variables.Method;

public class NoMethodMatchedException(List<Type> argumentTypes, List<(MethodInfo, ConstantExpression?)> candidates)
    : Exception("No method matches by arguments signature in candidate list")
{
    public List<Type> ArgumentTypes => argumentTypes;
    public List<(MethodInfo, ConstantExpression?)> Candidates => candidates;
}