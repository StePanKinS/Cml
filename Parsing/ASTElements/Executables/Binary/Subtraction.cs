namespace Cml.Parsing;

internal class Subtraction(Executable left, Executable right, Location location) : Binary(left, right, location)
{
    public new const int Priority = 4;
}