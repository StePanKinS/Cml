namespace Cml.Parsing;

internal class BitwiseXor(Executable left, Executable right, Location location) : Executable(location)
{
    public Executable Left = left;
    public Executable Right = right;

    public override int Priority => 9;
    public override bool IsRightToLeft => false;
}