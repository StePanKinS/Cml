using System.Diagnostics.CodeAnalysis;

namespace Cml.NameContexts;

public class FunctionArguments(INameContainer parent, FunctionDefinition func) : INameContainer
{
    public INameContainer Parent = parent;
    public List<VariableDefinition> Arguments = [];
    public FunctionDefinition ParentFunction = func;

    public int Size => 0;

    public bool Append(Definition definition)
    {
        if (definition is not VariableDefinition variable)
            throw new ArgumentException($"Only {nameof(VariableDefinition)} can be added to {nameof(FunctionArguments)}");

        if ((from def in (IEnumerable<Definition>)Arguments
             where def.Name == definition.Name
             select def).ToArray().Length > 0)
            return false;

        Arguments.Add(variable);
        return true;
    }

    public bool Append(Token[] type, Token<string> name)
            => Append(new VariableDefinition(name.Value, type,
                ParentFunction, [], new Location(type.TokensLocation(), name.Location)));

    public bool TryGetName(string name, [MaybeNullWhen(false)] out Definition definition)
    {
        var defs = (from def in (IEnumerable<Definition>)Arguments
                    where def.Name == name
                    select def).ToArray();
        if (defs.Length == 1)
        {
            definition = defs[0];
            return true;
        }

        if (defs.Length > 1)
            throw new Exception($"Found several `{name}` names");

        return Parent.TryGetName(name, out definition);
    }

    public bool TryGetType(string name, [MaybeNullWhen(false)] out Typ type)
        => Parent.TryGetType(name, out type);
}
