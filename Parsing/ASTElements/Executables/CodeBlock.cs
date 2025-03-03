namespace Cml.Parsing;

internal class CodeBlock(List<Executable> code, NameContext locals, Location location) : Executable(location)
{
    public List<Executable> Code = code;
    public NameContext LocalVariables = locals;
}
