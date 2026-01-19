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
            var imports = Imports.Select(i => i.Namespace == id.Namespace).ToArray();
            if (imports.Length == 1)
                return false;
            else if (imports.Length > 1)
                throw new Exception("multiple identical imports found");

            Imports.Add(id);
            return true;
        }

        return base.Append(definition);
    }

    protected override IEnumerable<Definition> getAllWithName(string name)
        => Project.SelectMany(file => file)
            .Concat(Imports.SelectMany(i => i.Namespace ?? (IEnumerable<Definition>)[]))
            .Where(def => def.Name == name);
}
