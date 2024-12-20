using Cml.Lexing;

namespace Cml.Parsing;

internal class FunctionCall(Executable funcPtr, List<Executable> args, Location location) : Executable(location)
{
    public Executable FuncPtr = funcPtr;
    public List<Executable> Args = args;
    
    public new const int Priority = 1;
    public override bool IsRightToLeft => false;
}
