namespace Cml.Lexing;

public class FloatLiteralToken(double value, Location location) : LiteralToken<double>(value, location);