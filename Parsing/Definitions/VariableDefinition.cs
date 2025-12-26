namespace Cml.Parsing.Definitions;

public class VariableDefinition : Definition
{
    public VariableDefinition(string name, Token[] typeName, Definition parent, Keywords[] modifyers, Location location)
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

    public Token[]? TypeName;
    public Typ Type;
}
