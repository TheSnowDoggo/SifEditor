using SCENeo;
using SCENeo.Ui;

namespace SifEditor;

internal sealed class Alert : IRenderSource
{
    private readonly TextLabel _label;

    private double _timer;

    public Alert()
    {
        _label = new TextLabel()
        {
            Width = 50,
            Height = 2,
            TextBgColor = SCEColor.Transparent,
            Anchor = Anchor.Bottom,
            TextWrapping = TextWrapping.Word,
        };
    }

    public void Update(double delta)
    {
        if (_timer > 0)
        {
            _timer -= delta;
            return;
        }

        _label.Visible = false;
    }

    public IEnumerable<IRenderable> Render()
    {
        return [_label];
    }

    public void Show(string message, SCEColor color = SCEColor.Red)
    {
        if (!Program.Config.AlertsEnabled)
        {
            return;
        }

        _label.Text = message;

        _label.BasePixel = new Pixel(color);
        _label.TextFgColor = color.Contrast();

        _timer = 3;

        _label.Visible = true;
    }
}