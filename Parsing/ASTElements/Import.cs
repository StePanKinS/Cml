namespace Cml.Parsing;

internal class ImportDefinition(string file, Location keywordLocation, Location location) : Definition(file, location)
{
    public string File = file;
    public Location KeywordLocation = keywordLocation;
}
