namespace Cml.Parsing.Definitions;

public class VariableDefinition : Definition
{
    public VariableDefinition(string name, string typeName, Definition parent, Keywords[] modifyers, Location location)
        : base(name, parent, modifyers, location)
    {
        TypeName = typeName;
        Type = null!;
    }

    public VariableDefinition(string name, Typ type, Definition parent, Keywords[] modifyers, Location location)
        : base(name, parent, modifyers, location)
    {
        Type = type;
    }

    public string? TypeName;
    public Typ Type;
}
