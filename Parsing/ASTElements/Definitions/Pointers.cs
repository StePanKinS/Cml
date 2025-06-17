namespace Cml.Parsing;

internal class Pointer(StructDefinition pointsTo) : StructDefinition(pointsTo.Name + '*', [], Location.Nowhere);

internal class FunctionPointer(StructDefinition returnType, StructDefinition[] args) : StructDefinition(getName(returnType, args), [], Location.Nowhere)
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
        => $"FuncPtr({getName(ReturnType, Args)})";

}
