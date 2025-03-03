using Cml.Lexing;

namespace Cml;

public static class Util
{
    public static void Exit(Token token, string message)
        => Exit(token.Location, message);

    public static void Exit(Location location, string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine();
        Console.Write($"{location}: ");
        Console.ResetColor();
        Console.WriteLine(message);

        Environment.Exit(1);

        while (true) ;
    }
}
