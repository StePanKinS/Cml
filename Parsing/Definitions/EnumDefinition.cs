using Cml.Types;
using Cml.Lexing;

namespace Cml.Parsing.Definitions;

public class EnumDefinition(
    string name,
    Typ underlyingType,
    (string name, long value)[] members,
    Definition parent,
    Keywords[] modifyers,
    Location location
) : Definition(name, parent, modifyers, location), ITypeContainer
{
    public EnumType EnumType { get; set; } = new(name, underlyingType, members);
    public Typ Type { get => EnumType; }
}
