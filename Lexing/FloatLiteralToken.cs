namespace Cml.Lexing;

public class FloatLiteralToken(double value, Location loc) : LiteralToken<double>(value, loc);