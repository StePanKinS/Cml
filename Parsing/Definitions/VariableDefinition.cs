namespace Cml.Parsing.Definitions;

public class VariableDefinition : Definition
{
    public VariableDefinition(string name, string typeName, Definition parent, Keywords[] modifyers, Location location)
        : base(name, parent, modifyers, location)
    {
        TypeName = typeName;
        ValueType = null!;
    }

    public VariableDefinition(string name, StructDefinition type, Definition parent, Keywords[] modifyers, Location location)
        : base(name, parent, modifyers, location)
    {
        ValueType = type;
    }

    public string? TypeName;
    public StructDefinition ValueType;
}
