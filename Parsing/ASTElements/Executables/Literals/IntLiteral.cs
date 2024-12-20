using Cml.Lexing;

namespace Cml.Parsing;

internal class IntLiteral(IntLiteralToken value) : Literal<ulong>(value);
