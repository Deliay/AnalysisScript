
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using AnalysisScript.Parser.Ast.Basic;

namespace AnalysisScript.Interpreter.Variables;

public class VariableMap : IEnumerable<KeyValuePair<AsIdentity, IContainer>>
{
    private readonly Dictionary<AsIdentity, IContainer> VarMap = [];
    private readonly Dictionary<string, AsIdentity> NameMap = [];

    public void Add(AsIdentity id, IContainer container)
    {
        VarMap.Add(id, container);
        NameMap.Add(id.Name, id);
    }

    public void Update(AsIdentity newId)
    {
        var val = VarMap[newId];
        VarMap.Remove(newId);
        VarMap.Add(newId, val);
    }

    public bool ContainsKey(AsIdentity id)
    {
        return VarMap.ContainsKey(id);
    }

    public IEnumerator<KeyValuePair<AsIdentity, IContainer>> GetEnumerator() => VarMap.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => VarMap.GetEnumerator();

    public bool TryGetValue(AsIdentity id, [NotNullWhen(true)] out IContainer container)
    {
        return VarMap.TryGetValue(id, out container!);
    }

    public bool TryGetValue(string name, [NotNullWhen(true)] out IContainer container)
    {
        container = null!;

        return NameMap.TryGetValue(name, out var id)
            && VarMap.TryGetValue(id, out container!);
    }

}