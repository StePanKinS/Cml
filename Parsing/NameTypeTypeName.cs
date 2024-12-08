using Cml.Lexing;

namespace Cml.Parsing;

internal class NameTypeTypeName(NameToken name, NameToken typeName, StructDefinition? type = null)
{
    public NameToken Name  = name;
    public StructDefinition? Type = type;
    public NameToken TypeName = typeName;
}
