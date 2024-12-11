using Cml.Lexing;

namespace Cml.Parsing;

internal class AddressOf(NameToken name, Location location) : Executable(location)
{
    public NameToken name;

    public override int Priority => 2;
    public override bool IsRightToLeft => true;
}