using Cml.Lexing;

namespace Cml;

public static class Util
{
    public static void Exit(Token token, string message)
    {
        Console.WriteLine($"{token.Location}: {message}");
    }
}
