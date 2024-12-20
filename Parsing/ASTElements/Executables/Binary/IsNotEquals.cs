namespace Cml.Parsing;

internal class IsNotEquals(Executable left, Executable right, Location location) : Binary(left, right, location)
{
    public new const int Priority = 7;
}