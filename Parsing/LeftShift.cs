namespace Cml.Parsing;

internal class LeftShift(Executable left, Executable right, Location location) : Executable(location)
{
    public Executable Left = left;
    public Executable Right = right;

    public override int Priority => 5;
    public override bool IsRightToLeft => false;
}