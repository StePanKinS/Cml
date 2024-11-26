namespace Cml.Lexing;

public class NameToken(string name, Location loc) : Token(loc)
{
    public string Name = name;

    public override string ToString()
        => $"NameToken({Name})";
}