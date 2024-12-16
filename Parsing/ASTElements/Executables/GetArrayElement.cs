namespace Cml.Parsing;

internal class GetArrayElement(Executable arrayPtr, Executable index, Location location) : Executable(location)
{
    public Executable ArrayPtr = arrayPtr;
    public Executable Index = index;
    
    public override int Priority => 1;
    public override bool IsRightToLeft => false;
}