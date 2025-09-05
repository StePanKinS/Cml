namespace Cml.Parsing.Definitions;

public class FunctionDefinition(string name, string retTypeName, NameContext args, Executable? code, Definition parent, Location location)
    : Definition(name, parent, location)
{
    public string ReturnTypeName = retTypeName;
    public NameContext Args = args;
    public StructDefinition? ReturnType;
    public Executable? Code = code;
}
