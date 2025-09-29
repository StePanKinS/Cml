namespace Cml.Parsing.Definitions;

public class FunctionDefinition(
    string name,
    Token<string> retTypeName,
    (Token<string>, Token<string>)[] args,
    Token[] unparsedCode,
    Definition parent,
    Keywords[] modifyers,
    Location location
) : Definition(name, parent, modifyers, location)
{
    public Token<string> ReturnTypeName = retTypeName;
    public StructDefinition ReturnType = null!;

    // TODO: create function argument class
    public (Token<string> type, Token<string> name)[] NotypeArgs = args;
    public NameContext Args = null!;
    
    public Token[] UnparsedCode = unparsedCode;
    public Executable? Code = null;
}
