namespace Cml.Parsing.Definitions;

public class FunctionDefinition(
    string name,
    Token[] retTypeName,
    Definition parent,
    Keywords[] modifyers,
    Typ? methodOf = null
) : Definition(name, parent, modifyers, Location.Nowhere)
{
    public Token[] ReturnTypeName = retTypeName;
    public Typ ReturnType = null!;

    public FunctionArguments Arguments = null!;
    public bool IsVariadic = false;

    public Typ? MethodOf = methodOf;

    public Token[] UnparsedCode = null!;
    public Executable? Code = null;

    public int LocalsSize = 0;

    public bool ContainsReturn = false;
}
