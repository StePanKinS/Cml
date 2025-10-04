namespace Cml.Parsing.Definitions;

public class FunctionDefinition(
    string name,
    Token<string> retTypeName,
    // Token[] unparsedCode,
    Definition parent,
    Keywords[] modifyers
    // Location location
) : Definition(name, parent, modifyers, Location.Nowhere)
{
    public Token<string> ReturnTypeName = retTypeName;
    public StructDefinition ReturnType = null!;

    public FunctionArguments Arguments = null!;

    public Token[] UnparsedCode = null!;
    public Executable? Code = null;
}
