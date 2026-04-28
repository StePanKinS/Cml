namespace Cml.Parsing.Definitions;

public class InterfaceDefinition : Definition, ITypeContainer
{
    public InterfaceType InterfaceType;
    public Typ Type { get => InterfaceType; }
    public (string name, Token[] retType, Token[][] parameters)[] MethodTokens = [];

    public InterfaceDefinition(string name, IEnumerable<(string, FunctionPointer)> methods, Definition parent, Keywords[] modifyers, Location location)
        : base(name, parent, modifyers, location)
    {
        InterfaceType = new InterfaceType(name, methods);
    }
}
