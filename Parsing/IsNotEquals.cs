namespace Cml.Parsing;

internal class IsNotEquals(Executable left, Executable right, Location location) : Executable(location)
{
    public Executable Left = left;
    public Executable Right = right;

    public override int Priority => 7;
    public override bool IsRightToLeft => false;
}