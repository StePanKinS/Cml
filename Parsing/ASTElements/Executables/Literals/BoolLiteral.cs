using Cml.Lexing;

namespace Cml.Parsing;

internal class BoolLiteral(BoolLiteralToken token) : Literal<bool>(token);
