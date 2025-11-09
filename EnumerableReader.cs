using System.Diagnostics.CodeAnalysis;

namespace Cml;

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
        val = next;
        return hasValue;
    }

    public bool Read([MaybeNullWhen(false)] out T val)
    {
        val = next;

        bool ret = hasValue;

        if (enumerator.MoveNext())
            next = enumerator.Current;
        else
            hasValue = false;

        return ret;
    }

    public bool Read()
    {
        if (enumerator.MoveNext())
            next = enumerator.Current;
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