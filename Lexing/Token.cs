namespace Cml.Lexing;

public sealed class Token<T>(T value, TokenType type, Location location, bool isFString = false) : Token(type, location, isFString)
{
    public T Value = value;

    public override string ToString()
        => $"{Type}({Value})";
}

public abstract class Token(TokenType type, Location location, bool isFString = false) : Location.ILocatable
{
    public TokenType Type = type;
    public Location Location { get; } = location;
    public bool IsFString = isFString;
}
