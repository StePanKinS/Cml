namespace Cml.Parsing;

internal class Or(Executable left, Executable right, Location location) : Binary(left, right, location)
{
    public override int Priority => 12;
}