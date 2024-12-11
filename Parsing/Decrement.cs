namespace Cml.Parsing;

internal class Decrement(Executable valuePtr, Location location) : Executable(location)
{
    public Executable ValuePtr = valuePtr;

    public override int Priority => 2;
    public override bool IsRightToLeft => true;
}