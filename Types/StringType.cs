namespace Cml.Types;

public class StringType : Typ
{
    public static StringType Instance { get; } = new("string", 16);

    public static DefaultType.Integer Byte { get; } = DefaultType.Integer.Byte;
    public static DefaultType.Integer Long { get; } = DefaultType.Integer.Long;

    private StringType(string name, int size) : base(name, size) { }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj);

    public override int GetHashCode()
        => base.GetHashCode();

    public override string ToString()
        => $"StringType";
}
