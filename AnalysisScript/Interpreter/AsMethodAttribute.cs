namespace AnalysisScript.Interpreter;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class AsMethodAttribute : Attribute
{
    public required string Name { get; set; }
}
