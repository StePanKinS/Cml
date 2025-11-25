using System.Diagnostics.CodeAnalysis;

namespace Cml.NameContexts;

public class LocalVariables(INameContainer parent) : INameContainer
{
    public INameContainer Parent = parent;
    public List<VariableDefinition> Variables = [];

    private int size = -1;
    public int Size
    {
        get
        {
            if (size == -1)
                size = Variables.Sum(v => (v.Type.Size + 7) & ~7);
            return size + Parent.Size;
        }
    }

    public int SelfSize
    {
        get
        {
            if (size == -1)
                size = Variables.Sum(v => (v.Type.Size + 7) & ~7);
            return size;
        }
    }

    public bool Append(Definition definition)
    {
        if (definition is not VariableDefinition variable)
            throw new ArgumentException($"Only {nameof(VariableDefinition)} can be added to {nameof(LocalVariables)}");

        if ((from def in (IEnumerable<Definition>)Variables
             where def.Name == definition.Name
             select def).ToArray().Length > 0)
            return false;

        Variables.Add(variable);
        return true;
    }

    public bool TryGetName(string name, [MaybeNullWhen(false)] out Definition definition)
    {
        var defs = (from def in (IEnumerable<Definition>)Variables
                    where def.Name == name
                    select def).ToArray();
        if (defs.Length == 1)
        {
            definition = defs[0];
            return true;
        }

        if (defs.Length > 1)
            throw new Exception($"Found several `{name}` names");

        return Parent.TryGetName(name, out definition);
    }

    public int GetVariableOffset(VariableDefinition variable)
    {
        if (!Variables.Contains(variable))
            return Parent.GetVariableOffset(variable);

        int offset = Parent.Size;
        foreach (var v in Variables)
        {
            offset += (v.Type.Size + 7) & ~7;

            if (v == variable)
                return -offset;
        }

        throw new Exception("How did we get here. GetVariableOffset");
    }

    public bool TryGetType(string name, [MaybeNullWhen(false)] out Typ type)
        => Parent.TryGetType(name, out type);
}