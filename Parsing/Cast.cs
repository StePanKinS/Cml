using Cml.Lexing;

namespace Cml.Parsing;

internal class Cast(NameToken typeName, Executable value, Location location) : Executable(location)
{
    public NameToken TypeName = typeName;
    public Executable Value = value;

    public override int Priority => 2;
    public override bool IsRightToLeft => true;
}