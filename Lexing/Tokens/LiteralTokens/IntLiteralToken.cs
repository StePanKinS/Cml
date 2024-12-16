namespace Cml.Lexing;

public class IntLiteralToken(ulong value, Location location) : LiteralToken<ulong>(value, location);