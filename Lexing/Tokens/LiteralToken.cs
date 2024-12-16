namespace Cml.Lexing;

public abstract class LiteralToken<T>(T value, Location location) : Token<T>(value, location);