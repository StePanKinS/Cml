namespace Cml.Parsing;

internal class Or(Executable left, Executable right, Location location) : Binary(left, right, location)
{
    public new const int Priority = 12;
}