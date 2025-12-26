using System.Text;

namespace Cml.Types;

public class FunctionPointer(Typ returnType, Typ[] args) : Typ(getName(returnType, args), 8)
{
    public FunctionPointer(FunctionDefinition funcDef)
        : this(funcDef.ReturnType, funcDef.Arguments.Arguments.Select(a => a.Type).ToArray())
    { }

    public Typ ReturnType = returnType;
    public Typ[] Args = args;

    private static string getName(Typ returnType, Typ[] args)
    {
        StringBuilder sb = new();
        sb.Append($"fn {returnType.Name}(");
        for (int i = 0; i < args.Length - 1; i++)
        {
            sb.Append(args[i].Name);
            sb.Append(',');
        }
        if (args.Length > 0)
            sb.Append(args[^1].Name);
        sb.Append(')');
        return sb.ToString();
    }

    public string GetName()
        => getName(ReturnType, Args);

    public override string ToString()
        => $"FunctionPointer({GetName()})";
}
