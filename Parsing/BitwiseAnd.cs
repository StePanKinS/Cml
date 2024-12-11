namespace Cml.Parsing;

internal class BitwiseAnd(Executable left, Executable right, Location location) : Executable(location)
{
    public Executable Left = left;
    public Executable Right = right;

    public override int Priority => 8;
    public override bool IsRightToLeft => false;
}