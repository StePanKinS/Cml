namespace Cml.Types;

public class InterfaceType(string name, IEnumerable<(string name, FunctionPointer)> methods) : Typ(name, 16)
{
    public (string name, FunctionPointer method)[] Methods = methods.ToArray();

    public FunctionPointer? GetMethod(string name)
    {
        var methods = (from m in Methods where m.name == name select m).ToArray();
        if (methods.Length > 1)
            throw new Exception("Several interface methods with the same name");
        if (methods.Length == 1)
            return methods[0].method;
        return null;
    }

    public int GetMethodIndex(string name)
    {
        for (int i = 0; i < Methods.Length; i++)
        {
            if (Methods[i].name == name)
                return i;
        }
        return -1;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not InterfaceType it)
            return false;
        return it.Name == Name;
    }

    public override int GetHashCode() => base.GetHashCode();

    public override string ToString() => $"Interface({Name})";
}
