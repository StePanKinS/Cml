namespace Cml.Lexing;

public class KeywordToken(Keywords keyword, Location location) : Token<Keywords>(keyword, location);
