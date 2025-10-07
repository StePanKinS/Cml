namespace Cml;

public class DefaultTypes(string name) : StructDefinition(name, [], null!, [], Location.Nowhere)
{
    public static DefaultTypes Void { get; } = new("void") { size = 0 };
    public static DefaultTypes Char { get; } = new("char") { size = 1 };
    public static DefaultTypes Int { get; } = new("int") { size = 8 };
    public static DefaultTypes Bool { get; } = new("bool") { size = 8 };

    public override string ToString()
        => $"DefaultType({Name})";
}