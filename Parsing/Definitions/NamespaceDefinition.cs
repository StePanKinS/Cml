using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Cml.Parsing.Definitions;

public class NamespaceDefinition : Definition, IEnumerable<Definition>
{
    public NameContext NameContext;
    public NamespaceDefinition(string name, NamespaceDefinition parent, Keywords[] modifyers, Location location)
        : base(name, parent, modifyers, location)
    {
        NameContext = new(parent?.NameContext!, this);
    }

    public virtual bool Append(Definition definition)
        => NameContext.Append(definition);

    public virtual bool Append(IEnumerable<NamespaceDefinition> nmspDefs)
        => NameContext.Append(nmspDefs.Select(n => n.NameContext));

    public virtual bool TryGetType(string name, [MaybeNullWhen(false)] out Typ definition)
        => NameContext.TryGetType(name, out definition);

    public virtual bool TryGetName(string name, [MaybeNullWhen(false)] out Definition definition)
        => NameContext.TryGetName(name, out definition);

    // public static NamespaceDefinition NewGlobal()
    //     => new("@global", null, [], Location.Nowhere);

    public IEnumerator<Definition> GetEnumerator()
        => NameContext.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
