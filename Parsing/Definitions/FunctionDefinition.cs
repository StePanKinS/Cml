namespace Cml.Parsing.Definitions;

public class FunctionDefinition(
    string name,
    Token[] retTypeName,
    Definition parent,
    Keywords[] modifyers
) : Definition(name, parent, modifyers, Location.Nowhere)
{
    public Token[] ReturnTypeName = retTypeName;
    public Typ ReturnType = null!;

    public FunctionArguments Arguments = null!;

    public Token[] UnparsedCode = null!;
    public Executable? Code = null;

    public bool ContainsReturn = false;
}
