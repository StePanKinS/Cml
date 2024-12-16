namespace Cml.Parsing;

internal abstract class Unary(Executable value, Location location) : Executable(location)
{
    public Executable Name = value;

    public override int Priority => 2;
    public override bool IsRightToLeft => true;
}