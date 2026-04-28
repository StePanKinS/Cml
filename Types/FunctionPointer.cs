using System.Text;

namespace Cml.Types;

public class FunctionPointer : Typ
{
    public FunctionPointer(FunctionDefinition funcDef)
        : this(funcDef.ReturnType, funcDef.Arguments.Arguments.Select(a => a.Type).ToArray(), funcDef.IsVariadic)
    { }

    public FunctionPointer(Typ returnType, Typ[] args, bool isVariadic = false)
        : this(returnType, args, isVariadic, getName(returnType, args, isVariadic), 8)
    { }

    protected FunctionPointer(Typ returnType, Typ[] args, bool isVariadic, string name, int size)
        : base(name, size)
    { 
        ReturnType = returnType;
        Args = args;
        IsVariadic = isVariadic;
    }

    public Typ ReturnType;
    public Typ[] Args;
    public bool IsVariadic;

    protected static string getName(Typ returnType, Typ[] args, bool isVariadic)
        => $"fn {getSignatureName(returnType, args, isVariadic)}";

    protected static string getSignatureName(Typ returnType, Typ[] args, bool isVariadic)
    {
        StringBuilder sb = new();
        sb.Append($"{returnType.Name}(");
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
