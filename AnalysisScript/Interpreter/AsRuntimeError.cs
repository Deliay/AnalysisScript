namespace AnalysisScript.Interpreter;

public enum AsRuntimeError
{
    Unknown,
    InvalidReturnType,
    VariableNotInitialized,
    VariableAlreadyExist,
    NotEnumerable,
    NoMatchedMethod,
}