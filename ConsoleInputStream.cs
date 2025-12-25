using System.Text;

namespace SifEditor;

internal sealed class ConsoleInputStream
{
    private readonly StringBuilder _sb = new();

    public void Next(ConsoleKeyInfo cki)
    {
        char c = cki.KeyChar;

        switch (cki.Key)
        {
        case ConsoleKey.Backspace:
            if (_sb.Length == 0)
            {
                break;
            }

            _sb.Remove(_sb.Length - 1, 1);

            break;
        case ConsoleKey.Spacebar:
            _sb.Append(' ');

            break;
        default:
            if (char.IsControl(c))
            {
                break;
            }

            _sb.Append(c);

            break;
        }
    }

    public override string ToString()
    {
        return _sb.ToString();
    }
}
