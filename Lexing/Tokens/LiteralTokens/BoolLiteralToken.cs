namespace Cml.Lexing;

public class BoolLiteralToken(bool value, Location location) : LiteralToken<bool>(value, location);