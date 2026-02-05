using SCENeo;
using SCENeo.Ui;

namespace SifEditor;

internal sealed class TextPrompt : IRenderSource
{
    public enum Result
    {
        String,
        Integer,
        Double,
    }

    private const double BlinkRate = 0.5;

    private readonly TextLabel _label;
    private readonly DisplayMap _cursor;
    private readonly VirtualOverlay _overlay;

    private readonly ConsoleInputStream _inputStream = new();

    private string _message = string.Empty;

    private Result _result;

    private Action<object>? _callback = null;

    private double _blinkTimer;

    public TextPrompt()
    {
        _label = new TextLabel()
        {
            Width = 80,
            Height = 3,
            Anchor = Anchor.Center | Anchor.Middle,
            TextWrapping = TextWrapping.Character,
            Visible = false,
        };

        _cursor = new DisplayMap(1, 1)
        {
            [0, 0] = new Pixel('\0', SCEColor.Black, SCEColor.White),
        };

        _overlay = new VirtualOverlay()
        {
            Source  = _label,
            Overlay = _cursor,
        };
    }

    public Alert? Alert { get; set; }

    public bool Visible { get { return _label.Visible; } }

    public void OnInput(ConsoleKeyInfo cki)
    {
        switch (cki.Key)
        {
        case ConsoleKey.Escape:
            Exit();
            break;
        case ConsoleKey.Enter:
            Enter();
            break;
        default:
            if (!_inputStream.Next(cki))
            {
                break;
            }

            UpdateText();
            UpdateCursorOffset();

            break;
        }
    }

    public bool Open(string message, Result result, string start, string end, Action<object> callback)
    {
        if (_label.Visible)
        {
            return false;
        }

        _message = message;
        _callback = callback;
        _result = result;

        _inputStream.Append(start);
        _inputStream.Append(end);

        _inputStream.CharacterIndex = start.Length;

        UpdateText();
        UpdateCursorOffset();

        _label.Visible = true;

        return true;
    }

    public bool Open(string message, Result result, Action<object> callback)
    {
        return Open(message, result, string.Empty, string.Empty, callback);
    }

    public bool Open(string message, string start, string end, Action<object> callback)
    {
        return Open(message, Result.String, start, end, callback);
    }

    public bool Open(string message, Action<object> callback)
    {
        return Open(message, Result.String, callback);
    }

    public IEnumerable<IRenderable> Render()
    {
        return [_overlay];
    }

    public void Update(double delta)
    {
        if (_blinkTimer < BlinkRate)
        {
            _blinkTimer += delta;
            return;
        }

        _blinkTimer = 0;

        _cursor.Visible = !_cursor.Visible;
    }

    private void Exit()
    {
        ExitPrompt();
    }

    private void Enter()
    {
        if (_callback == null)
        {
            Exit();
            return;
        }

        object? result = GetResult();

        if (result == null)
        {
            return;
        }

        var callback = _callback;

        ExitPrompt();

        callback.Invoke(result);
    }

    private object? GetResult()
    {
        string str = _inputStream.ToString();

        switch (_result)
        {
        case Result.String:
            return str;
        case Result.Integer:
        {
            if (int.TryParse(str, out int value))
            {
                return value;
            }
            Alert?.Show("Enter a valid integer.");
            break;
        }
        case Result.Double:
        {
            if (double.TryParse(str, out double value))
            {
                return value;
            }
            Alert?.Show("Enter a valid double.");
            break;
        }
        }

        return null;
    }

    private void UpdateCursorOffset()
    {
        int index = _message.Length + _inputStream.CharacterIndex;

        _cursor.Offset = new Vec2I(index % _label.Width, index / _label.Width);

        // Reset blink
        _cursor.Visible = true;
        _blinkTimer = 0;
    }

    private void UpdateText()
    {
        string text = _message + _inputStream.ToString();

        int maxCharacters = _label.Width * _label.Height;

        if (text.Length <= maxCharacters)
        {
            _label.Text = text;
            return;
        }

        _label.Text = $"{_label.Text[..(maxCharacters - 3)]}...";
    }

    private void ExitPrompt()
    {
        _label.Visible = false;

        _inputStream.Clear();

        _label.Text = string.Empty;
        _message = string.Empty;
        _callback = null;
    }
}