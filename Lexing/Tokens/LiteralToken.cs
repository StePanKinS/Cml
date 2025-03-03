namespace Cml.Lexing;

public interface ILiteralToken { }
public abstract class LiteralToken<T>(T value, Location location) : Token<T>(value, location), ILiteralToken;