namespace Cml.Types;

public class EnumType(string name, Typ underlyingType, (string name, long value)[] members) : Typ(name, underlyingType.Size)
{
    public Typ UnderlyingType = underlyingType;
    public (string Name, long Value)[] Members = members;

    public override bool Equals(object? obj)
    {
        if (obj is not EnumType et)
            return false;
        return et.Name == Name && et.UnderlyingType == UnderlyingType;
    }

    public override int GetHashCode() => base.GetHashCode();

    public override string ToString() => $"EnumType({Name} : {UnderlyingType.Name})";
}
