namespace Cml.Parsing.Definitions;

public abstract class Definition(string name, Definition parent, Keywords[] modifyers, Location location) : Location.ILocatable
{
    public string Name { get; } = name;
    public Definition Parent = parent;
    public Location Location { get; set; } = location;
    public Keywords[] Modifyers = modifyers;

    public virtual string FullName
    {
        get => Parent?.parentConstructName + Name;
    }

    protected virtual string parentConstructName
    {
        get => FullName + ".";
    }
}
