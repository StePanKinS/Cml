namespace Cml.Lexing;

internal class CharLiteralToken(char value, Location localtion) : LiteralToken<char>(value, localtion);
