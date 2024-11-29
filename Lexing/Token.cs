namespace Cml.Lexing;

public abstract class Token<T>(T value, Location location) : Token(location)
{
    public T Value = value;

    public override string ToString()
        => $"{GetType().Name}({Value})";
}

public abstract class Token(Location location)
{
    public Location Location = location;
}