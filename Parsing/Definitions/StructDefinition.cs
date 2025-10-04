namespace Cml.Parsing.Definitions;

public class StructDefinition(
    string name,
    IEnumerable<(Token<string> type, Token<string> name)> members,
    Definition parent,
    Keywords[] modifyers,
    Location location
) : Definition(name, parent, modifyers, location)
{
    public List<StructMember> Members = new(from def in members select
        new StructMember() { Name = def.name, TypeName = def.type, Type = null! });

    protected int size = -1;
    public int Size
    {
        get
        {
            if (size != -1)
                return size;

            size = 0;
            foreach (var m in Members)
            {
                size += m.Type.Size;
            }
            size = (size + 7) & ~7; // Align to 8 bytes
            return size;
        }
    }

    public StructMember? GetStructMember(string name)
    {
        var mems = (from mem in Members where mem.Name.Value == name select mem).ToArray();
        if (mems.Length > 1)
            throw new Exception("Several structure members with the same name");
        if (mems.Length == 1)
            return mems[0];
        return null;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not StructDefinition sd)
            return false;

        if (sd.Name != Name)
            return false;
        if (sd.Members.Count != Members.Count)
            return false;
        for (int i = 0; i < Members.Count; i++)
        {
            if (!Members[i].Equals(sd.Members[i]))
                return false;
        }
        return true;
    }

    public override int GetHashCode()
        => base.GetHashCode();

    public static void AddStandartTypes(NamespaceDefinition globalNamespace)
    {
        globalNamespace.Append(new StructDefinition("void", [], globalNamespace, [], Location.Nowhere) { size = 0 });
        globalNamespace.Append(new StructDefinition("char", [], globalNamespace, [], Location.Nowhere) { size = 1 });
        globalNamespace.Append(new StructDefinition("int", [], globalNamespace, [], Location.Nowhere) { size = 8 });
    }

    public static bool operator ==(StructDefinition? sd1, StructDefinition? sd2)
        => sd1?.Equals(sd2) ?? false;
    public static bool operator !=(StructDefinition? sd1, StructDefinition? sd2)
        => !(sd1?.Equals(sd2) ?? false);

    public class StructMember
    {
        // This field can be null during the definition-reading step. After that it must be non null
        // I dont want to check for null every time in code parsing so it would be non-nullable
        public required StructDefinition Type;
        public required Token<string> Name;
        public required Token<string> TypeName;

        public override bool Equals(object? obj)
        {
            if (obj is not StructMember sm)
                return false;
            return sm.Name == Name && sm.Type == Type;
        }

        public override int GetHashCode()
            => base.GetHashCode();
    }
}
