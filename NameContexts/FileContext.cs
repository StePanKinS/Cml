using System.Diagnostics.CodeAnalysis;

namespace Cml.NameContexts;

public class FileContext : NameContext
{
    public IEnumerable<FileDefinition> Project;

    public FileContext(IEnumerable<FileDefinition> project)
        : base(null!)
    {
        Project = project;
    }

    public override bool TryGetType(string name, [MaybeNullWhen(false)] out Typ type)
        => tryGetType(name, out type, true);

    private bool tryGetType(string name, [MaybeNullWhen(false)] out Typ type, bool search)
    {
        if (base.TryGetType(name, out type))
            return true;
        if (!search)
            return false;

        foreach (var f in Project)
        {
            var fc = (FileContext)f.NameContext;
            if (ReferenceEquals(fc, this))
                continue;

            if (fc.tryGetType(name, out type, false))
                return true;
        }
        return false;
    }

    public override bool TryGetName(string name, [MaybeNullWhen(false)] out Definition definition)
        => tryGetName(name, out definition, true);

    private bool tryGetName(string name, [MaybeNullWhen(false)] out Definition definition, bool search)
    {
        if (base.TryGetName(name, out definition))
            return true;
        if (!search)
            return false;

        foreach (var f in Project)
        {
            var fc = (FileContext)f.NameContext;
            if (ReferenceEquals(fc, this))
                continue;

            if (fc.tryGetName(name, out definition, false))
                return true;
        }
        return false;
    }
}
