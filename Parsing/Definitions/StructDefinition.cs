namespace Cml.Parsing.Definitions;

public class StructDefinition(
    string name,
    IEnumerable<(Token<string> type, Token<string> name)> members,
    Definition parent,
    Keywords[] modifyers,
    Location location
) : Definition(name, parent, modifyers, location)
{
    public (Token<string> type, Token<string> name)[] Members = members.ToArray();
    public StructType Type = null!;
}
