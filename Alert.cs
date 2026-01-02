using SCENeo;
using SCENeo.Ui;

namespace SifEditor;

internal sealed class Alert : IRenderSource
{
    private readonly TextLabel _tb;

    private double _timer;

    public Alert()
    {
        _tb = new TextLabel()
        {
            Width = 50,
            Height = 2,
            TextBgColor = SCEColor.Transparent,
            Anchor = Anchor.Bottom,
            TextWrapping = TextLabel.Wrapping.Word,
        };
    }

    public void Show(string message, SCEColor color = SCEColor.Red)
    {
        _tb.Text = message;

        _tb.BasePixel = new Pixel(color);
        _tb.TextFgColor = color.Contrast();

        _timer = 3;

        _tb.Visible = true;
    }

    public void Update(double delta)
    {
        if (_timer > 0)
        {
            _timer -= delta;
            return;
        }

        _tb.Visible = false;
    }

    public IEnumerable<IRenderable> Render()
    {
        return [_tb];
    }
}