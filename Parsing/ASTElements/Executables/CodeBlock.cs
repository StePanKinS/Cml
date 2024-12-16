namespace Cml.Parsing;

internal class CodeBlock(List<Executable> code, Location location) : Executable(location)
{
    public List<Executable> Code = code;
}
