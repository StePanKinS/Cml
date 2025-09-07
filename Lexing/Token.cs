namespace Cml.Lexing;

public sealed class Token<T>(T value, TokenType type, Location location) : Token(type, location)
{
    public T Value = value;

    public override string ToString()
        => $"{Type}({Value})";
}

public abstract class Token(TokenType type, Location location)
{
    public TokenType Type = type;
    public Location Location = location;
}