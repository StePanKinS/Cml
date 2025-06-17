using System.Diagnostics;

namespace Cml.Parsing;

internal class NameContext(NameContext? parent)
{
    public NameContext? Parent = parent;
    public Dictionary<string, Definition> Names = [];

    public Definition? GetValue(string name)
    {
        if (name[^1] == '*')
        {
            Definition? definition = GetValue(name[..^1]);

            if (definition is not StructDefinition structDef)
                return null;

            return new Pointer(structDef);
        }

        if (Names.TryGetValue(name, out Definition? value))
                return value;

        if (Parent != null)
            return Parent.GetValue(name);

        return null;
    }

    public bool Add(Definition def)
        => Names.TryAdd(def.Name, def);
}
