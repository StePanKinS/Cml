using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Cml.NameContexts;

public class NameContext(NameContext parent) : IEnumerable<Definition>, INameContainer
{
    public NameContext Parent = parent;
    public List<NamespaceDefinition> Namespaces = [];
    public List<StructDefinition> Structs = [];
    public List<FunctionDefinition> Functions = [];
    public List<VariableDefinition> Variables = [];
    public List<DefaultTypeDefinition> DefaultTypes = [];

    public bool Append(Definition definition)
    {
        if ((from def in (IEnumerable<Definition>)[.. Namespaces, .. Structs,
            .. Functions, .. Variables, ..DefaultTypes] where def.Name == definition.Name 
            select def).ToArray().Length > 0)
            return false;

        switch (definition)
        {
            case NamespaceDefinition nmsp:
                Namespaces.Add(nmsp);
                return true;
            case StructDefinition struc:
                Structs.Add(struc);
                return true;
            case FunctionDefinition func:
                Functions.Add(func);
                return true;
            case VariableDefinition variable:
                Variables.Add(variable);
                return true;
            case DefaultTypeDefinition deft:
                DefaultTypes.Add(deft);
                return true;
            default:
                throw new ArgumentException($"Unexpected type {definition.GetType().FullName} in {nameof(definition)}");
        }
    }

    public virtual bool TryGetName(string name, [MaybeNullWhen(false)] out Definition definition)
    {
        var defs = (from def in (IEnumerable<Definition>)[.. Namespaces, .. Structs, 
                    .. Functions, .. Variables, .. DefaultTypes]
                    where def.Name == name
                    select def).ToArray();
        if (defs.Length == 1)
        {
            definition = defs[0];
            return true;
        }

        if (defs.Length > 1)
            throw new Exception($"Found several `{name}` names");

        if (Parent != null)
            return Parent.TryGetName(name, out definition);

        definition = null;
        return false;
    }

    public virtual bool TryGetType(string name, [MaybeNullWhen(false)] out Typ definition)
    {
        definition = default;
        int ptrCnt = 0;

        foreach (char c in name)
        {
            if (c == '*')
                ptrCnt++;
        }
        name = name[..(name.Length - ptrCnt)];

        var sdefs = Structs.Where((s) => s.Name == name).Select(s => s.Type);
        var ddefs = DefaultTypes.Where(s => s.Name == name).Select(s => s.Type);
        Typ[] defs = [.. sdefs, .. ddefs];

        if (defs.Length == 1)
            definition = defs[0];
        else
        {
            if (defs.Length > 1)
                throw new Exception($"Found several `{name}` names");

            if (Parent == null || !Parent.TryGetType(name, out definition))
                return false;
        }

        for (int i = 0; i < ptrCnt; i++)
        {
            definition = new Pointer(definition);
        }
        return true;
    }

    public int GetVariableOffset(VariableDefinition variable)
        => 0;

    public int Size => 0;

    public IEnumerator<Definition> GetEnumerator()
        => ((IEnumerable<Definition>)[.. Namespaces, .. Structs, .. Functions, .. Variables]).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
