namespace Cml.Lexing;

public class IntLiteralToken(ulong value, Location loc) : LiteralToken<ulong>(value, loc);