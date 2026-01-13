namespace Cml.Parsing.Definitions;

public class FileDefinition : NamespaceDefinition
{
    public IEnumerable<FileDefinition> Project;

    public FileDefinition(string name, IEnumerable<FileDefinition> project, Keywords[] modifyers, Location location)
        : base(name, null!, modifyers, location)
    {
        Project = project;
        NameContext = new FileContext(project);
    }

    protected override string ParentConstructName
    {
        get => string.Empty;
    }
}
