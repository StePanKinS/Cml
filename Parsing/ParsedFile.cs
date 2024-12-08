namespace Cml.Parsing;

internal class ParsedFile(string path, List<Definition> definitions, List<Import> imports)
{
    public string Path = path;
    public List<Definition> Definitions = definitions;
    public List<Import> Imports = imports;
}