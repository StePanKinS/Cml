using System.Text;

namespace Cml.Parsing.Definitions;

public class Pointer : StructDefinition
{
    public StructDefinition PointsTo;

    public Pointer(StructDefinition pointsTo)
        : base(pointsTo.Name + '*', [], null!, [], Location.Nowhere)
    {
        PointsTo = pointsTo;
        size = 8;
    }

    public override string ToString()
        => $"Pointer({PointsTo.Name})";

    public static bool operator ==(Pointer? p1, Pointer? p2)
        => (p1 is null && p2 is null) || (p1 is not null && p2 is not null && p1.PointsTo == p2.PointsTo);
    public static bool operator !=(Pointer? p1, Pointer? p2)
        => !(p1 == p2);

    public override bool Equals(object? obj)
        => obj is Pointer p && p == this;

    public override int GetHashCode()
        => base.GetHashCode();
}

public class FunctionPointer(StructDefinition returnType, StructDefinition[] args)
    : StructDefinition(getName(returnType, args), [], null!, [], Location.Nowhere)
{
    public FunctionPointer(FunctionDefinition funcDef)
        : this(funcDef.ReturnType, funcDef.Arguments.Variables.Select(a => a.ValueType).ToArray())
    { }

    public StructDefinition ReturnType = returnType;
    public StructDefinition[] Args = args;

    private static string getName(StructDefinition returnType, StructDefinition[] args)
    {
        StringBuilder sb = new();
        sb.Append($"fn {returnType.Name}(");
        for (int i = 0; i < args.Length - 1; i++)
        {
            sb.Append(args[i].Name);
            sb.Append(',');
        }
        sb.Append(args[^1].Name);
        sb.Append(')');
        return sb.ToString();
    }

    public string GetName()
        => getName(ReturnType, Args);

    public override string ToString()
        => $"FunctionPointer({GetName()})";
}
