namespace Cml.Parsing;

internal class DivisionReminder(Executable left, Executable right, Location location) : Binary(left, right, location)
{
    public override int Priority => 3;
}