namespace Cml.Parsing;

internal class BitwiseAnd(Executable left, Executable right, Location location) : Binary(left, right, location)
{
    public new const int Priority = 8;
}