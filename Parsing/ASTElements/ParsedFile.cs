namespace Cml.Parsing;

internal class ParsedFile(string path, NameContext definitions, List<Import> imports)
{
    public string Path = path;
    //public List<Definition> Definitions = definitions;
    public NameContext Definitions = definitions;
    public List<Import> Imports = imports;
}