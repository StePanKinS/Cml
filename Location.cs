namespace Cml;

public class Location(string file, int sLine, int sCol, int eLine, int eCol)
{
    public string File = file;

    // Starting from 0
    public int StartLine = sLine;
    public int EndLine = eLine; // Inclusive
    public int StartColumn = sCol;
    public int EndColumn = eCol; // Exclusive

    public Location(Location start, Location end) : this
    (
        start.File,
        start.StartLine,
        start.StartColumn,
        end.EndLine,
        end.EndColumn
    )
    { }

    public Location(Location.ILocatable start, Location.ILocatable end)
        : this(start.Location, end.Location) { }

    public static readonly Location Nowhere = new(null!, -1, -1, -1, -1);

    public void Set(Location loc)
    {
        StartLine = loc.StartLine;
        EndLine = loc.EndLine;
        StartColumn = loc.StartColumn;
        EndColumn = loc.EndColumn;
        File = loc.File;
    }

    public override string ToString()
        => $"{File}:{StartLine + 1}:{StartColumn + 1} - {EndLine + 1}:{EndColumn + 1}";

    public interface ILocatable
    {
        public Location Location { get; }
    }
}
