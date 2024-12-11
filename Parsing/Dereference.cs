namespace Cml.Parsing;

internal class Dereference(Executable ptr, Location location) : Executable(location)
{
    public Executable Pointer = ptr;

    public override int Priority => 2;
    public override bool IsRightToLeft => true;
}