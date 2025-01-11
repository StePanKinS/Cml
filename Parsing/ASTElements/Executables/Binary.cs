namespace Cml.Parsing;

internal abstract class Binary(Executable left, Executable right, Location location) : Executable(location)
{
    public Executable Left = left;
    public Executable Right = right;

    public new const bool IsRightToLeft = false;
}
