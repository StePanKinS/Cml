namespace Cml.Parsing;

internal class ParsedFile(string path, NameContext definitions)
{
    public string Path = path;
    public NameContext Definitions = definitions;
    // public List<Import> Imports = imports;
}
