namespace Cml.Parsing;

internal class IsLess(Executable left, Executable right, Location location) : Executable(location)
{
    public Executable Left = left;
    public Executable Right = right;

    public override int Priority => 6;
    public override bool IsRightToLeft => false;
}