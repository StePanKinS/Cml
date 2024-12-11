namespace Cml.Parsing;

internal class Assign(Executable address, Executable right, Location location) : Executable(location)
{
    public Executable Address = address;
    public Executable Value = right;
    
    public override int Priority => 13;
    public override bool IsRightToLeft => true;
}