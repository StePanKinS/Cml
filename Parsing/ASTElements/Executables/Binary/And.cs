namespace Cml.Parsing;

internal class And(Executable left, Executable right, Location location) : Binary(left, right, location)
{
    public override int Priority => 11;
}