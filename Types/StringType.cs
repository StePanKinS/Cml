namespace Cml.Types;

public class StringType : Typ
{
    public static StringType Instance { get; } = new("string", 16);

    public static DefaultType.Integer Byte { get; } = DefaultType.Integer.Byte;
    public static DefaultType.Integer Long { get; } = DefaultType.Integer.Long;

    public FunctionDefinition? ToCMethod { get; private set; }
    public FunctionDefinition? FromCMethod { get; private set; }

    private StringType(string name, int size) : base(name, size) { }

    public void SetMethods(FunctionDefinition toC, FunctionDefinition fromC)
    {
        ToCMethod = toC;
        FromCMethod = fromC;
    }

    public FunctionDefinition? GetMethod(string name)
    {
        if (name == "to_c" && ToCMethod != null)
            return ToCMethod;
        if (name == "from_c" && FromCMethod != null)
            return FromCMethod;
        return null;
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj);

    public override int GetHashCode()
        => base.GetHashCode();

    public override string ToString()
        => $"StringType";
}
