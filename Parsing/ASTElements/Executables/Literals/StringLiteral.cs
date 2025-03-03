using Cml.Lexing;

namespace Cml.Parsing;

internal class StringLiteral(StringLiteralToken value) : Literal<string>(value);
