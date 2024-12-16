namespace Cml.Parsing;

internal class Decrement(Executable value, bool isPostfix, Location location) : Unary(value, location)
{
    public bool IsPostfix = isPostfix;
}