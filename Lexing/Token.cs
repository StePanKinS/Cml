namespace Cml.Lexing;

public abstract class Token(Location loc)
{
    public Location Location = loc;
}