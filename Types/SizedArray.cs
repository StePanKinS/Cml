namespace Cml.Types;

public class SizedArray(Typ type, int size) : Typ($"{type.Name}[{size}]", type.Size * size)
{
    public Typ ElementType = type;
    public int ElementCount = size;

    public override bool Equals(object? obj)
    {
        if (obj is not SizedArray other)
            return false;
        return ElementType == other.ElementType && ElementCount == other.ElementCount;
    }

    public override int GetHashCode()
        => base.GetHashCode();
}
