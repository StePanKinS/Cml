namespace Cml.Lexing;

public class BoolLiteralToken(bool value, Location loc) : LiteralToken<bool>(value, loc);