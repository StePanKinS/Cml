namespace Cml.Lexing;

public class StringLiteralToken(string value, Location location) : LiteralToken<string>(value, location);