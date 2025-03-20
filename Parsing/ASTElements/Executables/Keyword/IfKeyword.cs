using Cml.Lexing;

namespace Cml.Parsing;

internal class IfKeyword(Executable condition, Executable body, Executable? elseBody, Location location) : Keyword(Keywords.If, location)
{
    public Executable Condition { get; set; } = condition;
    public Executable Body { get; set; } = body;
    public Executable? IfBody { get; set; } = elseBody;
}
