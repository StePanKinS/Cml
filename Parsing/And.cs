namespace Cml.Parsing;

internal class And(Executable left, Executable right, Location location) : Executable(location)
{
    public Executable Left = left;
    public Executable Right = right;

    public override int Priority => 11;
    public override bool IsRightToLeft => false;
}