using Cml.Lexing;

namespace Cml.Parsing;

internal class ValueOf(NameToken name, Location location) : Executable(location)
{
    public NameToken Name = name;
    
    public new const int Priority = 0;
    public new const bool IsRightToLeft = false;
}
