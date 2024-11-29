namespace Cml.Lexing;

public class NameToken(string name, Location location) : Token<string>(name, location);