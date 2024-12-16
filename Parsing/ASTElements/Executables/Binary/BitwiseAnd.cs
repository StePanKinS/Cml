namespace Cml.Parsing;

internal class BitwiseAnd(Executable left, Executable right, Location location) : Binary(left, right, location)
{
    public override int Priority => 8;
}