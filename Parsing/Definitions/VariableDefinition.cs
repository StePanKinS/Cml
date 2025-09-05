namespace Cml.Parsing.Definitions;

public class VariableDefinition : Definition
{
    public VariableDefinition(string name, string typeName, Definition parent, Location location)
        : base(name, parent, location)
    {
        TypeName = typeName;
        ValueType = null!;
    }

    public VariableDefinition(string name, StructDefinition type, Definition parent, Location location)
        : base(name, parent, location)
    {
        ValueType = type;
    }

    public string? TypeName;
    public StructDefinition ValueType;
}
