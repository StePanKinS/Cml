namespace Cml.Parsing;

internal class FunctionDefinition(string name, string retTypeName, NameContext args, Executable? code, NameContext? nameCtx, bool isExtern, Location location) : Definition(name, location)
{
    public string ReturnTypeName = retTypeName;
    public NameContext Args = args;
    public StructDefinition? ReturnType;
    public Executable? Code = code;
    public bool IsExtern = isExtern;
    public NameContext? LocalNameContext = nameCtx;
}
