namespace Cml.Parsing.Definitions;

public abstract class Definition(string name, Definition parent, Keywords[] modifyers, Location location)
{
    public string Name = name;
    public Definition Parent = parent;
    public Location Location = location;
    public Keywords[] Modifyers = modifyers;

    public string FullName
    {
        get
        {
            if (Parent == null || Parent.Name == "@global")
                return Name;
            return Parent.FullName + "." + Name;
        }
    }
}
