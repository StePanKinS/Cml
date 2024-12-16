namespace Cml.Parsing;

internal abstract class Definition(string name, Location location) : ASTElement(location)
{
    public string Name = name;
}
