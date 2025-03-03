using Cml.Lexing;

namespace Cml.Parsing;

public interface ILiteral { }
internal class Literal<T>(LiteralToken<T> token) : Executable(token.Location), ILiteral
{
    public LiteralToken<T> LiteralToken = token;
    public T Value = token.Value;

    public new const int Priority = 0;
    public new const bool IsRightToLeft = false;
}
