using System.Text;

namespace Cml.Parsing.Definitions;

public class Pointer(StructDefinition pointsTo) : StructDefinition(pointsTo.Name + '*', [], null!, Location.Nowhere);

public class FunctionPointer(StructDefinition returnType, StructDefinition[] args)
    : StructDefinition(getName(returnType, args), [], null!, Location.Nowhere)
{
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
