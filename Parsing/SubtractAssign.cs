namespace Cml.Parsing;

internal class SubtractAssign(Executable address, Executable value, Location location) : Executable(location)
{
    public Executable Address = address;
    public Executable Value = value;

    public override int Priority => 13;
    public override bool IsRightToLeft => true;
}