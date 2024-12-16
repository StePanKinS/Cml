namespace Cml.Parsing;

internal class Import(string file, Location keywordLocation, Location location) : ASTElement(location)
{
    public string File = file;
    public Location KeywordLocation = keywordLocation;
}
