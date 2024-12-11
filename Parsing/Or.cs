namespace Cml.Parsing;

internal class Or(Executable left, Executable right, Location location) : Executable(location)
{
    public Executable Left = left;
    public Executable Right = right;

    public override int Priority => 12;
    public override bool IsRightToLeft => false;
}