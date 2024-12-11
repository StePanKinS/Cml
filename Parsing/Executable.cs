namespace Cml.Parsing;

internal abstract class Executable(Location location, StructDefinition? returnType = null) : ASTElement(location)
{
    public StructDefinition? ReturnType = returnType;

    public static int Priority = -1;
}
