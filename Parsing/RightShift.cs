namespace Cml.Parsing;

internal class RightShift(Executable left, Executable right, Location location) : Executable(location)
{
    public Executable Left = left;
    public Executable Right = right;

    public override int Priority => 5;
    public override bool IsRightToLeft => false;
}