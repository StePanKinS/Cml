namespace Cml.Parsing;

internal class Invert(Executable value, Location location) : Executable(location)
{
    public Executable Value = value;

    public new const int Priority = 2;
    public override bool IsRightToLeft => true;
}