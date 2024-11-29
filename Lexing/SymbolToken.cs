namespace Cml.Lexing;

public class SymbolToken(Symbols symbol, Location location) : Token<Symbols>(symbol, location);