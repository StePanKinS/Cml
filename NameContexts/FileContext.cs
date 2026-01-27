namespace Cml.NameContexts;

public class FileContext : NameContext
{
    public IEnumerable<FileDefinition> Project;
    public List<ImportDefinition> Imports = [];

    public FileContext(IEnumerable<FileDefinition> project, FileDefinition fileDef)
        : base(null!, fileDef)
    {
        Project = project;
    }

    public override bool Append(Definition definition)
    {
        if (definition is ImportDefinition id)
        {
            var imports = Imports.Where(i => i.Name == id.Name).ToArray();
            if (imports.Length > 0)
                return false;

            Imports.Add(id);
            return true;
        }

        return base.Append(definition);
    }

    protected override IEnumerable<Definition> getAllWithName(string name)
        => Project.SelectMany(file => file)
            .Concat(Imports.Where(i => i.Namespace != null).SelectMany(i => i.Namespace!))
            .Where(def => def.Name == name);
}
