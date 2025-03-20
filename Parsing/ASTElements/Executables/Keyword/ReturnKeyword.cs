using Cml.Lexing;

namespace Cml.Parsing;

internal class ReturnKeyword(Executable value, Location location) : Keyword(Keywords.Return, location)
{
    public Executable Value = value;
}
