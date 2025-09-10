namespace Cml.Parsing.Definitions;

public class StructDefinition(
    string name,
    IEnumerable<(string type, string name)> members,
    Definition parent,
    Keywords[] modifyers,
    Location location
) : Definition(name, parent, modifyers, location)
{
    public List<StructMember> Members = new(from def in members select
        new StructMember() { Name = def.name, TypeName = def.type, Type = null! });

    public StructMember? GetStructMember(string name)
    {
        var mems = (from mem in Members where mem.Name == name select mem).ToArray();
        if (mems.Length > 1)
            throw new Exception("Several structure members with the same name");
        if (mems.Length == 1)
            return mems[0];
        return null;
    }

    public static void AddStandartTypes(NamespaceDefinition globalNamespace)
    {
        globalNamespace.Append(new StructDefinition("void", [], globalNamespace, [], Location.Nowhere));
        globalNamespace.Append(new StructDefinition("char", [], globalNamespace, [], Location.Nowhere));
        globalNamespace.Append(new StructDefinition("int", [], globalNamespace, [], Location.Nowhere));
    }

    public class StructMember
    {
        // This field can be null during the definition-reading step. After that it must be non null
        // I dont want to check for null every time in code parsing so it would be non-nullable
        public required StructDefinition Type;
        public required string Name;
        public required string TypeName;
    }
}
