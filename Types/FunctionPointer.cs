using System.Text;

namespace Cml.Types;

public class FunctionPointer(Typ returnType, Typ[] args, bool isVariadic = false) : Typ(getName(returnType, args, isVariadic), 8)
{
    public FunctionPointer(FunctionDefinition funcDef)
        : this(funcDef.ReturnType, funcDef.Arguments.Arguments.Select(a => a.Type).ToArray(), funcDef.IsVariadic)
    { }

    public Typ ReturnType = returnType;
    public Typ[] Args = args;
    public bool IsVariadic = isVariadic;

    private static string getName(Typ returnType, Typ[] args, bool isVariadic)
    {
        StringBuilder sb = new();
        sb.Append($"fn {returnType.Name}(");
        for (int i = 0; i < args.Length; i++)
        {
            sb.Append(args[i].Name);
            if (i != args.Length - 1 || isVariadic)
                sb.Append(", ");
        }
        if (isVariadic)
            sb.Append("...");
        sb.Append(')');
        return sb.ToString();
    }

    public string GetName()
        => getName(ReturnType, Args, IsVariadic);

    public override string ToString()
        => $"FunctionPointer({GetName()})";
}
