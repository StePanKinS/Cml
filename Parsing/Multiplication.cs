namespace Cml.Parsing;

internal class Multiplication(Executable left, Executable right, Location location) : Executable(location)
{
    public Executable Left = left;
    public Executable Right = right;

    public override int Priority => 3;
    public override bool IsRightToLeft => false;
}