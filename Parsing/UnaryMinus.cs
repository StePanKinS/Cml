namespace Cml.Parsing;

internal class UnaryMinus(Executable value, Location location) : Executable(location)
{
    public Executable Value = value;

    public override int Priority => 2;
    public override bool IsRightToLeft => true;
}