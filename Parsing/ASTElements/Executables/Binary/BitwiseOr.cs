namespace Cml.Parsing;

internal class BitwiseOr(Executable left, Executable right, Location location) : Binary(left, right, location)
{
    public new const int Priority = 10;
}