namespace Cml;

public class CmlProject(
    string name,
    string baseDir,
    string[] sources,
    string? output = null,
    string? tmpBuildDir = null,
    bool cleanBuild = false,
    bool printTokens = false
)
{
    public string Name = name;
    public string BaseDir = baseDir;
    public string[] Sources = sources;
    public string Output = output ?? Path.Combine(baseDir, name);
    public string TmpBuildDir = tmpBuildDir ?? $"/tmp/{name}/";
    public bool CleanTmpFiles = cleanBuild;
    public bool PrintTokens = printTokens;
}
