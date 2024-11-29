namespace Cml.Lexing;

public abstract class StringLiteralToken(string value, Location location) : LiteralToken<string>(value, location);