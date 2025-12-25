using System.Collections;

namespace SifEditor;

internal sealed class ShiftBuffer<T> : IReadOnlyList<T>
{
    private readonly T[] _buffer;
    private int _head;

    public ShiftBuffer()
    {
        _buffer = [];
    }

    public ShiftBuffer(int length)
    {
        _buffer = new T[length];
    }

    public int Count { get { return _buffer.Length; } }

    public T this[int index]
    {
        get { return _buffer[Translate(index)]; }
        set { _buffer[Translate(index)] = value; }
    }

    public void Shift(int shift)
    {
        _head = Mod(_head + shift, _buffer.Length);
    }

    public void Remove(int start, int count)
    {
        for (int i = 0; i < count; i++)
        {
            this[start + i] = default!;
        }
    }

    public void Clear()
    {
        Array.Clear(_buffer);
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _buffer.Length; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private int Translate(int index)
    {
        return (_head + index) % _buffer.Length;
    }

    private static int Mod(int x, int y)
    {
        return ((x % y) + y) % y;
    }
}