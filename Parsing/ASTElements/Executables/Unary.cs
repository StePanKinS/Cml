namespace Cml.Parsing;

internal abstract class Unary(Executable value, Location location) : Executable(location)
{
    public Executable Name = value;

    public new const int Priority = 2;
    public new const bool IsRightToLeft = true;
}