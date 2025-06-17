using Cml.Lexing;
using System.Diagnostics.CodeAnalysis;

namespace Cml;

public static class Util
{
    public static void Exit(Token token, string message)
        => Exit(token.Location, message);

    [DoesNotReturn]
    public static void Exit(Location location, string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine();
        Console.Write($"{location} ");
        Console.ResetColor();
        Console.WriteLine(message);

        Environment.Exit(1);
    }

    public static IEnumerable<(int, T)> Enumerate<T>(this IEnumerable<T> enumerable)
    {
        IEnumerator<T> enumerator = enumerable.GetEnumerator();
        int i = 0;
        while (enumerator.MoveNext())
        {
            yield return (i++, enumerator.Current);
        }
    }
}
