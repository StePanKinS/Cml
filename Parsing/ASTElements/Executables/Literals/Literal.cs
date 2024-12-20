using Cml.Lexing;

namespace Cml.Parsing;

internal class Literal<T>(LiteralToken<T> token) : Executable(token.Location)
{
    public LiteralToken<T> LiteralToken = token;

    public new const int Priority = 0;
    public override bool IsRightToLeft => false;
}
