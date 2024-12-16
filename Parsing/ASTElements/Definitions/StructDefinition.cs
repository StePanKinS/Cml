namespace Cml.Parsing;

internal class StructDefinition(string name, List<NameTypeTypeName> members, Location location) : Definition(name, location)
{
    public List<NameTypeTypeName> Members = members;
}
