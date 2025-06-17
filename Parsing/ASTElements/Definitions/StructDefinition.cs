namespace Cml.Parsing;

internal class StructDefinition(string name, List<NameTypeTypeName> members, Location location) : Definition(name, location)
{
    public static StructDefinition Void = new("void", [], Location.Nowhere);
    public static StructDefinition Char = new("char", [], Location.Nowhere);
    public List<NameTypeTypeName> Members = members;
}
