namespace Cml.Parsing;

internal class IsLess(Executable left, Executable right, Location location) : Binary(left, right, location)
{
    public override int Priority => 6;
}