namespace Cml.Parsing;

internal class Addition(Executable left, Executable right, Location location) : Executable(location)
{
    public Executable Left = left;
    public Executable Right = right;

    public override int Priority => 4;
    public override bool IsRightToLeft => false;
}