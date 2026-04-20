namespace Cml.Parsing.Definitions;

public class InterfaceDefinition : Definition, ITypeContainer
{
    public InterfaceType.InterfaceMethod[] Methods;
    public InterfaceType InterfaceType;
    public Typ Type { get => InterfaceType; }

    public InterfaceDefinition(string name, IEnumerable<InterfaceType.InterfaceMethod> methods, Definition parent, Keywords[] modifyers, Location location)
        : base(name, parent, modifyers, location)
    {
        Methods = methods.ToArray();
        InterfaceType = new InterfaceType(name, Methods);
    }
}