namespace Cml.Parsing.Definitions;

public class StructDefinition(
    string name,
    IEnumerable<(Token[] type, Token<string> name)> members,
    Definition parent,
    Keywords[] modifyers,
    Location location
) : Definition(name, parent, modifyers, location), ITypeContainer
{
    public (Token[] type, Token<string> name)[] Members = members.ToArray();
    public StructType StructType { get; set; } =
        new(name, members.Select(m => new StructType.StructMember(m.name.Value, typeName: m.type)).ToArray());
    public Typ Type { get => StructType; }
}
