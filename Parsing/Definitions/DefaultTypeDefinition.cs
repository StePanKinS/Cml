namespace Cml.Parsing.Definitions;

public class DefaultTypeDefinition(Typ type)
        : Definition(type.Name, null!, [], Location.Nowhere)
{
    public Typ Type = type;
    public override string FullName => Name;
}