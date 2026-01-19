namespace Cml.Parsing.Definitions;

public class ImportDefinition : Definition
{
    public Token<string>[] NamespaceName;
    public NamespaceDefinition Namespace;

    public ImportDefinition(Token<string>[] name, Location location)
        : base(getName(name), null!, [], location)
    {
        NamespaceName = name;
        Namespace = null!;
    }

    private static string getName(Token<string>[] name)
        => string.Join('.', name.Select(n => n.Value));

    protected override string parentConstructName => throw new Exception("how da hell im a parent");
}
