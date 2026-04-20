namespace Cml.Types;

public class InterfaceType(string name, InterfaceType.InterfaceMethod[] methods) : Typ(name, -1)
{
    public InterfaceMethod[] Methods = methods;

    public InterfaceMethod? GetMethod(string name)
    {
        var methods = (from m in Methods where m.Name == name select m).ToArray();
        if (methods.Length > 1)
            throw new Exception("Several interface methods with the same name");
        if (methods.Length == 1)
            return methods[0];
        return null;
    }

    public int GetMethodIndex(string name)
    {
        for (int i = 0; i < Methods.Length; i++)
        {
            if (Methods[i].Name == name)
                return i;
        }
        throw new Exception($"Method {name} not found in interface {Name}");
    }

    public override bool Equals(object? obj)
    {
        if (obj is not InterfaceType it)
            return false;
        return it.Name == Name;
    }

    public override int GetHashCode() => base.GetHashCode();

    public override string ToString() => $"Interface({Name})";

    public class InterfaceMethod(string name, Typ returnType, Typ[] parameters)
    {
        public string Name = name;
        public Typ ReturnType = returnType;
        public Typ[] Parameters = parameters;

        public override bool Equals(object? obj)
            => obj is InterfaceMethod im && im.Name == Name && im.ReturnType == ReturnType && im.Parameters.SequenceEqual(Parameters);

        public override int GetHashCode() => base.GetHashCode();
    }
}