using Cml.Lexing;

namespace Cml.Parsing;

internal class FloatLiteral(FloatLiteralToken token) : Literal<double>(token);
