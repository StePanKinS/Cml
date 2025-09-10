namespace Cml;

public static class Util
{
    public static IEnumerable<(int index, T value)> Enumerate<T>(this IEnumerable<T> enumerable)
    {
        using var enumerator = enumerable.GetEnumerator();
        int i = 0;
        while (enumerator.MoveNext())
        {
            yield return (i++, enumerator.Current);
        }
    }

    public static EnumerableReader<T> GetReader<T>(this IEnumerable<T> enumerable)
        => new(enumerable);
}