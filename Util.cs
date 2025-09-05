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

    public static EnumerableReader<T> GetReader<T>(this IEnumerable<T> enumerable)
        => new(enumerable);
}


public class EnumerableReader<T>
{
    private IEnumerator<T> enumerator;
    private T next;
    private bool hasValue = false;

    public EnumerableReader(IEnumerable<T> enumerable)
    {
        enumerator = enumerable.GetEnumerator();
        next = default!;

        if (enumerator.MoveNext())
        {
            next = enumerator.Current;
            hasValue = true;
        }
    }

    public bool Peek([MaybeNullWhen(false)] out T val)
    {
        val = hasValue ? next : default;
        return hasValue;
    }

    public bool Read([MaybeNullWhen(false)] out T val)
    {
        val = hasValue ? next : default;

        if (enumerator.MoveNext())
        {
            next = enumerator.Current;
            hasValue = true;
        }
        else
            hasValue = false;

        return hasValue;
    }

    public void Reset()
    {
        enumerator.Reset();

        if (enumerator.MoveNext())
        {
            next = enumerator.Current;
            hasValue = true;
        }
        else
            hasValue = false;
    }
}