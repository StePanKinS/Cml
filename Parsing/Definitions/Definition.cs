namespace Cml.Parsing.Definitions;

public abstract class Definition(string name, Definition parent, Location location)
{
    public string Name = name;
    public Definition Parent = parent;
    public Location Location = location;
}
