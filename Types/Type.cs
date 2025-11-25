namespace Cml.Types;

public abstract class Typ(string name, int size)
{
    protected string name = name;
    protected int size = size;


    public virtual string Name { get => name; }
    public virtual int Size { get => size; }

    public static void AddStandartTypes(NamespaceDefinition globalNamespace)
    {
        var types = (IEnumerable<Typ>)[
            DefaultType.Void,
            DefaultType.Char,
            DefaultType.Bool,
            DefaultType.Integer.SByte,
            DefaultType.Integer.Short,
            DefaultType.Integer.Int,
            DefaultType.Integer.Long,
            DefaultType.Integer.Byte,
            DefaultType.Integer.UShort,
            DefaultType.Integer.UInt,
            DefaultType.Integer.ULong,
            DefaultType.FloatingPoint.Float,
            DefaultType.FloatingPoint.Double,
        ];

        foreach(var i in types)
        {
            globalNamespace.Append(new DefaultTypeDefinition(i));
        }
    }


    public static bool operator ==(Typ? sd1, Typ? sd2)
        => (sd1 is null && sd2 is null) || (sd1?.Equals(sd2) ?? false);
    public static bool operator !=(Typ? sd1, Typ? sd2)
        => !((sd1 is null && sd2 is null) || (sd1?.Equals(sd2) ?? false));


    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj);
    public override int GetHashCode()
        => base.GetHashCode();
    public override string ToString()
        => $"Type({Name})";
}
