namespace Cml.Parsing;

internal class Inverse(Executable value, Location location) : Executable(location)
{
    public Executable Value = value;

    public new const int Priority = 2;
    public new const bool IsRightToLeft = true;
}