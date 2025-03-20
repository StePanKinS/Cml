using Cml.Lexing;

namespace Cml.Parsing;

internal class CharLiteral(CharLiteralToken token) : Literal<char>(token);
