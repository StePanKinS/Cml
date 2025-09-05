namespace Cml.Parsing.Definitions;

public class Pointer(StructDefinition pointsTo) : StructDefinition(pointsTo.Name + '*', [], null!, Location.Nowhere);

public class FunctionPointer(StructDefinition returnType, StructDefinition[] args)
    : StructDefinition(getName(returnType, args), [], null!, Location.Nowhere)
{
    public StructDefinition ReturnType = returnType;
    public StructDefinition[] Args = args;

    private static string getName(StructDefinition returnType, StructDefinition[] args)
    {
        string s = $"fn {returnType.Name}(";
        for (int i = 0; i < args.Length - 1; i++)
        {
            s += args[i].Name;
            s += ',';
        }
        return $"{s}{args[^1].Name})";
    }

    public override string ToString()
        => $"FunctionPointer({getName(ReturnType, Args)})";

}
