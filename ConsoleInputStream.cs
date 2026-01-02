using System.Text;

namespace SifEditor;

internal sealed class ConsoleInputStream
{
    private readonly StringBuilder _sb = new();

    public int CharacterIndex { get; private set; }

    public bool Next(ConsoleKeyInfo cki)
    {
        char c = cki.KeyChar;

        switch (cki.Key)
        {
        case ConsoleKey.Backspace:
            if (_sb.Length == 0 || CharacterIndex == 0)
            {
                return false;
            }

            _sb.Remove(CharacterIndex - 1, 1);
            CharacterIndex--;

            break;
        case ConsoleKey.LeftArrow:
            if (CharacterIndex <= 0)
            {
                return false;
            }

            CharacterIndex--;

            break;
        case ConsoleKey.RightArrow:
            if (CharacterIndex >= _sb.Length)
            {
                return false;
            }

            CharacterIndex++;

            break;
        default:
            if (c < ' ')
            {
                return false;
            }

            if (CharacterIndex < _sb.Length)
            {
                _sb.Insert(CharacterIndex, c);
            }
            else
            {
                _sb.Append(c);
            }

            CharacterIndex++;

            break;
        }

        return true;
    }

    public void Clear()
    {
        CharacterIndex = 0;

        _sb.Clear();
    }

    public override string ToString()
    {
        return _sb.ToString();
    }
}
