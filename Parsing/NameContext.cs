namespace Cml.Parsing;

internal class NameContext(NameContext? parent)
{
    public NameContext? Parent = parent;
    public Dictionary<string, Definition> Names = [];

    public Definition? GetValue(string name)
    {
        if (Names.TryGetValue(name, out Definition? value))
            return value;

        if (Parent != null)
            return Parent.GetValue(name);

        return null;
    }
}
