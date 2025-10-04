namespace Cml.Errors;

public record Error(string Message, ErrorType Level, Location Location)
{
    public override string ToString()
        => $"{Location}: {Level}: {Message}";
}