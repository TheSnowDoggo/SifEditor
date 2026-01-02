using SCENeo;
using SCENeo.Ui;

namespace SifEditor;

internal sealed class Prompt : IRenderSource
{
    private const double BlinkRate = 0.5;

    private readonly TextBox _tb;
    private readonly DisplayMap _cursor;
    private readonly VirtualOverlay _overlay;

    private readonly ConsoleInputStream _inputStream = new();

    private string _message = string.Empty;

    private Action<string>? _callback = null;

    private double _blinkTimer;

    public Prompt()
    {
        _tb = new TextBox()
        {
            Width = 80,
            Height = 3,
            Anchor = Anchor.Center | Anchor.Middle,
            TextWrapping = TextBox.Wrapping.Character,
            Visible = false,
        };

        _cursor = new DisplayMap(1, 1)
        {
            [0, 0] = new Pixel('\0', SCEColor.Black, SCEColor.White),
        };

        _overlay = new VirtualOverlay()
        {
            Source  = _tb,
            Overlay = _cursor,
        };
    }

    public bool Visible { get { return _tb.Visible; } }

    public void OnInput(ConsoleKeyInfo cki)
    {
        switch (cki.Key)
        {
        case ConsoleKey.Escape:
            _tb.Visible = false;

            ExitPrompt();
            break;
        case ConsoleKey.Enter:
            _tb.Visible = false;

            _callback?.Invoke(_inputStream.ToString());

            ExitPrompt();
            break;
        default:
            if (!_inputStream.Next(cki))
            {
                break;
            }

            SetText(_message + _inputStream.ToString());
            UpdateCursorOffset();

            break;
        }
    }

    public bool Open(string message, Action<string>? callback = null)
    {
        if (_tb.Visible)
        {
            return false;
        }

        _message = message;
        _callback = callback;

        SetText(message);
        UpdateCursorOffset();

        _tb.Visible = true;

        return true;
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

    private void UpdateCursorOffset()
    {
        int index = _message.Length + _inputStream.CharacterIndex;

        _cursor.Offset = new Vec2I(index % _tb.Width, index / _tb.Width);

        // Reset blink
        _cursor.Visible = true;
        _blinkTimer = 0;
    }

    private void SetText(string text)
    {
        int maxCharacters = _tb.Width * _tb.Height;

        if (text.Length <= maxCharacters)
        {
            _tb.Text = text;
            return;
        }

        _tb.Text = $"{_tb.Text[..(maxCharacters - 3)]}...";
    }

    private void ExitPrompt()
    {
        _inputStream.Clear();

        _tb.Text = string.Empty;
        _message = string.Empty;
        _callback = null;
    }
}