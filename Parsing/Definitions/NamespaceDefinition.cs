namespace Cml.Parsing.Definitions;

public class NamespaceDefinition : Definition
{
    public NameContext NameContext;
    public NamespaceDefinition(string name, Definition? parent, Location location)
        : base(name, parent!, location)
    {
        if (parent == null)
            NameContext = new(null);
        else if (parent is NamespaceDefinition nmsp)
            NameContext = new(nmsp.NameContext);
        else
            throw new Exception($"Namespace could only be defined in global scope or other namespaces");
    }

    public List<NamespaceDefinition> Namespaces { get => NameContext.Namespaces; }
    public List<StructDefinition> Structs { get => NameContext.Structs; }
    public List<FunctionDefinition> Functions { get => NameContext.Functions; }
    public List<VariableDefinition> Variables { get => NameContext.Variables; }

    public bool Append(Definition definition)
        => NameContext.Append(definition);

    public static NamespaceDefinition NewGlobal()
        => new("@global", null, Location.Nowhere);
}
