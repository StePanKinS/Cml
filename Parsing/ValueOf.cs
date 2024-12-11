using Cml.Lexing;

namespace Cml.Parsing;

internal class ValueOf(NameToken name, Location location) : Executable(location)
{
    public NameToken Name = name;
    
    public override int Priority => 0;
    public override bool IsRightToLeft => false;
}
