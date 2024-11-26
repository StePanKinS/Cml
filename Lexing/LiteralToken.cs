namespace Cml.Lexing;

public abstract class LiteralToken<T>(T value, Location loc) : Token(loc)
{
    public T Value = value;

    public override string ToString()
        => $"LiteralToken({Value})";
}