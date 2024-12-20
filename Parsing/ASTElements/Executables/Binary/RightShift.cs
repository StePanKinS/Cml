namespace Cml.Parsing;

internal class RightShift(Executable left, Executable right, Location location) : Binary(left, right, location)
{
    public new const int Priority = 5;
}