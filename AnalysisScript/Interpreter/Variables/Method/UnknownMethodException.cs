namespace AnalysisScript.Interpreter.Variables.Method;

public class UnknownMethodException(string methodName, string signature) : Exception($"Unknown method: {methodName}({signature})")
{
}