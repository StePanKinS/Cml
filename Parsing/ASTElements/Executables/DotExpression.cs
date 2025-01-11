using Cml.Lexing;

namespace Cml.Parsing;

internal class Dot(Executable left, NameToken right, Location location) : Executable(location) 
{
    public Executable Left = left;
    public NameToken Right = right;
    
    public new const int Priority = 1;
    public new const bool IsRightToLeft = false;
}