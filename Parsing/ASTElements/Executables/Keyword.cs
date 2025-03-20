using Cml.Lexing;

namespace Cml.Parsing;

internal abstract class Keyword(Keywords value, Location location) : Executable(location)
{
    public Keywords Value { get; set; } = value;
}
