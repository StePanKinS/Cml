namespace Cml.Parsing;

internal class Addition(Executable left, Executable right, Location location) : Binary(left, right, location)
{
    public override int Priority => 4;
}