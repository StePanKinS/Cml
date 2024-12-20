namespace Cml.Parsing;

internal abstract class Executable(Location location, StructDefinition? returnType = null) : ASTElement(location)
{
    public StructDefinition? ReturnType = returnType;

    public const int Priority = -1;
    public abstract bool IsRightToLeft { get; }
}
