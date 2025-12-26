namespace Cml.Types;

public class SizedArray(Typ type, int size) : Typ($"{type.Name}[{size}]", type.Size * size)
{
    public Typ ElementType = type;
    public int ElementCount = size;
}
