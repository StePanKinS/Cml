namespace Cml.Parsing;

internal class BitwiseOr(Executable left, Executable right, Location location) : Executable(location)
{
    public Executable Left = left;
    public Executable Right = right;

    public override int Priority => 10;
    public override bool IsRightToLeft => false;
}