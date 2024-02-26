
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using AnalysisScript.Parser.Ast.Basic;

namespace AnalysisScript.Interpreter.Variables;

public class VariableMap : IEnumerable<KeyValuePair<AsIdentity, IContainer>>
{
    private readonly Dictionary<AsIdentity, IContainer> _varMap = [];
    private readonly Dictionary<string, AsIdentity> _nameMap = [];
    
    public void Add(AsIdentity id, IContainer container)
    {
        _varMap.Add(id, container);
        _nameMap.Add(id.Name, id);
    }

    public void Update(AsIdentity newId)
    {
        var val = _varMap[newId];
        _varMap.Remove(newId);
        _varMap.Add(newId, val);
    }

    internal void Unset(AsIdentity id)
    {
        _varMap.Remove(id);
        _nameMap.Remove(id.Name);
    }

    internal void UpdateReference(AsIdentity id, IContainer container)
    {
        _varMap[id] = container;
    }

    public bool ContainsKey(AsIdentity id)
    {
        return _varMap.ContainsKey(id);
    }

    public IEnumerator<KeyValuePair<AsIdentity, IContainer>> GetEnumerator() => _varMap.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _varMap.GetEnumerator();

    public bool TryGetValue(AsIdentity id, [NotNullWhen(true)] out IContainer container)
    {
        return _varMap.TryGetValue(id, out container!);
    }

    public bool TryGetValue(string name, [NotNullWhen(true)] out IContainer container)
    {
        container = null!;

        return _nameMap.TryGetValue(name, out var id)
            && _varMap.TryGetValue(id, out container!);
    }

    public void Clear()
    {
        _varMap.Clear();
        _nameMap.Clear();
    }
    
}