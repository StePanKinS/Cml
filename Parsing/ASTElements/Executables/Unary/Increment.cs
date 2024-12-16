namespace Cml.Parsing;

internal class Increment(Executable value, bool isPostfix, Location location) : Unary(value, location)
{
    public bool IsPostfix = isPostfix;
}