namespace Cml.Parsing;

internal abstract class Assign(Executable address, Executable value, Location location) : Executable(location)
{
    public Executable Address = address;
    public Executable Value = value;

    public new const int Priority = 13;
    public new const bool IsRightToLeft = true;
}
