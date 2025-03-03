namespace Cml.Parsing;

internal class VariableDefinition(string name, string typeName, Location location) : Definition(name, location)
{
    public string TypeName = typeName;
    public StructDefinition? ValueType;
}
