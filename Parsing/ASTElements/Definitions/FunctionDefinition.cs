namespace Cml.Parsing;

internal class FunctionDefinition(string name, string retTypeName, List<NameTypeTypeName> args, Executable code, Location location) : Definition(name, location)
{
    public string ReturnTypeName = retTypeName;
    public List<NameTypeTypeName> Args = args;
    public StructDefinition? ReturnType;
    public Executable Code = code;
}
