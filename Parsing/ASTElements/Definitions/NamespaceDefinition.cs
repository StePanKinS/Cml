namespace Cml.Parsing;

internal class NamespaceDefinition(string name, NameContext nameCtx, Location location) : Definition(name, location)
{
    public NameContext Definitions = nameCtx;
}
