namespace Cml.Parsing.Definitions;

public class DefaultTypeDefinition(Typ type)
        : Definition(type.Name, null!, [], Location.Nowhere), ITypeContainer
{
    public Typ Type { get; set; } = type;
    public override string FullName => Name;
    protected override string parentConstructName => throw new Exception("im not a parent!");
}
