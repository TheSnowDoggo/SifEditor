using SCEWin;
using SCENeo;
using System.Text;

namespace SifEditor;

internal class KeyInputStream
{
    private readonly StringBuilder _sb = new();

    public void OnInput(InputData inputData)
    {
        if (inputData.Type != MessageType.KeyDown)
        {
            return;
        }

        switch (inputData.Kbdll.Key)
        {
        case Key.Enter:
            _sb.AppendLine();
            return;
        case Key.Back:
            if (_sb.Length > 0)
            {
                _sb.Remove(_sb.Length - 1, 1);
            }
            return;
        case Key.Tab:
            _sb.Append('\t');
            return;
        }

        string unicode = inputData.Kbdll.ToUnicode();

        if (unicode.Length == 0)
        {
            return;
        }

        if (char.IsControl(unicode[0]))
        {
            return;
        }

        _sb.Append(unicode);
    }

    public void Clear()
    {
        _sb.Clear();
    }

    public override string ToString()
    {
        return _sb.ToString();
    }
}