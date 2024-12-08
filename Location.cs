namespace Cml;

public class Location(string file, int sLine, int eLine, int sCol, int eCol) {
    public string File = file;

    // Starting from 0
    public int StartLine = sLine;
    public int EndLine = eLine; // Inclusive
    public int StartColumn = sCol;
    public int EndColumn = eCol; // Exclusive

#pragma warning disable CS8625
    public static readonly Location Nowhere = new(null, -1, -1, -1, -1);
#pragma warning restore CS8625

    public override string ToString()
        => $"{File}:{StartLine}:{StartColumn}";
}