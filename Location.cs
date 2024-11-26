namespace Cml;

public class Location(string file, int sLine, int eLine, int sCol, int eCol) {
    public string File = file;

    // Starting from 0
    public int StartLine = sLine;
    public int EndLine = eLine;
    public int StartColumn = sCol;
    public int EndColumn = eCol;

#pragma warning disable CS8625
    public static readonly Location NoWhere = new(null, -1, -1, -1, -1);
#pragma warning restore CS8625
}