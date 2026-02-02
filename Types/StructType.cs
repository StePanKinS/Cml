namespace Cml.Types;

public class StructType(string name, StructType.StructMember[] members, FunctionDefinition[] methods) : Typ(name, -1)
{
    public StructMember[] Members = members;
    public FunctionDefinition[] Methods = methods;

    public override int Size {
        get {
            if (size != -1)
                return size;
            return size;
        }
    }

    public StructMember? GetStructMember(string name)
    {
        var mems = (from mem in Members where mem.Name == name select mem).ToArray();
        if (mems.Length > 1)
            throw new Exception("Several structure members with the same name");
        if (mems.Length == 1)
            return mems[0];
        return null;
    }

    public FunctionDefinition? GetMethod(string name)
    {
        var meths = (from meth in Methods where meth.Name == name select meth).ToArray();
        if (meths.Length > 1)
            throw new Exception("Several methods with the same name");
        if (meths.Length == 1)
            return meths[0];
        return null;
    }

    public int GetMemberOffset(string name)
    {
        int offset = 0;
        foreach (var member in Members)
        {
            if (member.Name == name)
                return offset;

            offset += member.Type.Size;
        }

        throw new ArgumentException($"Member with name {name} doesnt exist");
    }

    public override bool Equals(object? obj)
    {
        if (obj is not StructType sd)
            return false;

        if (sd.Name != Name)
            return false;
        if (sd.Members.Length != Members.Length)
            return false;
        for (int i = 0; i < Members.Length; i++)
        {
            if (!Members[i].Equals(sd.Members[i]))
                return false;
        }
        return true;
    }

    public override int GetHashCode()
        => base.GetHashCode();

    private static int calculateSize(StructType.StructMember[] members)
    {
        int size = 0;
        foreach (var i in members)
        {
            size += i.Type.Size;
        }
        return size;
    }

    public class StructMember(string name, Typ? type = null, Token[]? typeName = null)
    {
        public Typ Type = type!;
        public Token[] TypeName = typeName!;
        public string Name = name;

        public override bool Equals(object? obj)
            => obj is StructMember sm && sm.Name == Name && sm.Type == Type;

        public override int GetHashCode()
            => base.GetHashCode();
    }
}
