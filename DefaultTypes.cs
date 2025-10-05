namespace Cml;

public class DefaultTypes(string name) : StructDefinition(name, [], null!, [], Location.Nowhere)
{
    public static DefaultTypes Void { get; } = new("void");
    public static DefaultTypes Char { get; } = new("char");
    public static DefaultTypes Int { get; } = new("int");
}