using System.Diagnostics.CodeAnalysis;

namespace Cml.NameContexts;

public interface INameContainer
{
    public bool Append(Definition definition);
    public bool TryGet(string name, [MaybeNullWhen(false)] out Definition definition);
    public bool TryGetType(string name, [MaybeNullWhen(false)] out StructDefinition type);
    public int GetVariableOffset(VariableDefinition variable);
    public int Size { get; }
}
