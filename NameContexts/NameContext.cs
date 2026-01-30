using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Cml.NameContexts;

public class NameContext(NameContext parent, NamespaceDefinition nmspDef) : IEnumerable<Definition>, INameContainer
{
    public NameContext Parent = parent;
    public NamespaceDefinition NamespaceDefinition = nmspDef;
    public List<NamespaceDefinition> Namespaces = [];
    public List<StructDefinition> Structs = [];
    public List<FunctionDefinition> Functions = [];
    public List<VariableDefinition> Variables = [];
    public List<DefaultTypeDefinition> DefaultTypes = [];
    public List<EnumDefinition> Enums = [];

    public virtual bool Append(Definition definition)
    {
        if ((from def in (IEnumerable<Definition>)[.. Namespaces, .. Structs,
            .. Functions, .. Variables, ..DefaultTypes, ..Enums]
             where def.Name == definition.Name
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
            case EnumDefinition enu:
                Enums.Add(enu);
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

    public virtual bool Append(IEnumerable<NameContext> ctxs)
    {
        foreach (var c in ctxs)
        {
            foreach (var d in c)
            {
                if (!Append(d))
                    throw new Exception($"Could not append {d} {d.FullName}");
            }
        }
        return true;
    }

    public bool TryGetName(string name, [MaybeNullWhen(false)] out Definition definition)
        => tryGetName(name, out definition, true);

    private bool tryGetName(string name, [MaybeNullWhen(false)] out Definition definition, bool goUp)
    {
        var defs = getAllWithName(name).ToArray();

        if (defs.Length == 1)
        {
            definition = defs[0];
            return true;
        }

        if (defs.Length > 1)
        {
            foreach (var d in defs)
            {
                if (d is not NamespaceDefinition nmsp)
                    throw new Exception("Several names that are not namespaces found");
            }
            NamespaceDefinition n = new(defs[0].Name, NamespaceDefinition, [], Location.Nowhere);
            n.Append(defs.Cast<NamespaceDefinition>());
            definition = n;
            return true;
        }

        if (Parent != null)
        {
            if (Parent.TryGetName(name, out definition))
                return true;

            if (!goUp)
                return false;

            if (!Parent.TryGetName(NamespaceDefinition.Name, out var bigSelf))
                throw new Exception("How da hell im not a child of my parent");

            // namespace object can represent not whole namesspace,
            // this will enshure thar we are searching across whole namespace
            return ((NamespaceDefinition)bigSelf).NameContext.tryGetName(name, out definition, false);
        }

        definition = null;
        return false;
    }

    protected virtual IEnumerable<Definition> getAllWithName(string name)
    {
        IEnumerable<Definition> all = [
            .. Namespaces,
            .. Structs,
            .. Enums,
            .. Functions,
            .. Variables,
            .. DefaultTypes,
        ];
        return all.Where(d => d.Name == name);
    }

    public bool TryGetType(string name, [MaybeNullWhen(false)] out Typ type)
    {
        type = default;
        int ptrCnt = 0;

        foreach (char c in name)
        {
            if (c == '*')
                ptrCnt++;
        }
        name = name[..(name.Length - ptrCnt)];

        if (!TryGetName(name, out var def))
            return false;
        if (def is not ITypeContainer tc)
            return false;
        type = tc.Type;

        for (int i = 0; i < ptrCnt; i++)
        {
            type = new Pointer(type);
        }
        return true;
    }

    public int GetVariableOffset(VariableDefinition variable)
        => 0;

    public int Size => 0;

    public IEnumerator<Definition> GetEnumerator()
        => ((IEnumerable<Definition>)[.. Namespaces, .. Structs, .. Enums, .. Functions, .. Variables, .. DefaultTypes]).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
