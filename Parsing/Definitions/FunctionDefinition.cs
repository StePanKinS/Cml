namespace Cml.Parsing.Definitions;

public class FunctionDefinition(
    string name,
    string retTypeName,
    (string, string)[] args,
    Token[] unparsedCode,
    Definition parent,
    Keywords[] modifyers,
    Location location
) : Definition(name, parent, modifyers, location)
{
    public string ReturnTypeName = retTypeName;
    public StructDefinition ReturnType = null!;

    public (string, string)[] NotypeArgs = args;
    public NameContext Args = null!;
    
    public Token[] UnparsedCode = unparsedCode;
    public Executable? Code = null;
}
