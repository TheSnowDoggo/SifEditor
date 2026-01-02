using SCENeo;
using SCENeo.Ui;

namespace SifEditor;

internal sealed class Alert : IRenderSource
{
    private readonly TextBox _tb;

    private double _timer;

    public Alert()
    {
        _tb = new TextBox()
        {
            Width = 50,
            Height = 2,
            BasePixel = Pixel.DarkRed,
            TextFgColor = SCEColor.White,
            TextBgColor = SCEColor.Transparent,
            Anchor = Anchor.Bottom,
            TextWrapping = TextBox.Wrapping.Word,
        };
    }

    public void Show(string message, double time = 3)
    {
        _tb.Text = message;
        _timer = time;

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