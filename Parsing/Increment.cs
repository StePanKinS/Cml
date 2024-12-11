namespace Cml.Parsing;

internal class Increment(Executable valuePtr, bool isPostfix, Location location) : Executable(location)
{
    public Executable ValuePtr = valuePtr;
    public bool IsPostfix = isPostfix;

    
    public override int Priority => 2;
    public override bool IsRightToLeft => true;
}