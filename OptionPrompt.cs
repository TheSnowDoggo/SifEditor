using SCENeo;
using SCENeo.Ui;

namespace SifEditor;

internal sealed class OptionPrompt : IRenderSource
{
    private readonly VerticalSelector _vs;

    private Action<int>? _callback = null;

    public OptionPrompt()
    {
        _vs = new VerticalSelector()
        {
            Width = 30,
            Anchor = SCENeo.Anchor.Center | SCENeo.Anchor.Middle,
            Visible = false,
        };
    }

    public bool Visible { get { return _vs.Visible; } }

    public IEnumerable<IRenderable> Render()
    {
        yield return _vs;
    }

    public void OnInput(ConsoleKeyInfo cki)
    {
        switch (cki.Key)
        {
        case ConsoleKey.Escape:
            Exit();
            break;
        case ConsoleKey.Enter:
            Exit();
            _callback?.Invoke(_vs.Selected);
            break;
        case ConsoleKey.UpArrow:
            _vs.WrapMove(1);
            break;
        case ConsoleKey.DownArrow:
            _vs.WrapMove(-1);
            break;
        }
    }

    public void Open(UpdateCollection<VerticalSelector.Option> options, Action<int> callback)
    {
        _vs.Options = options;
        _vs.Height = options.Count;
        _vs.Visible = true;

        _callback = callback;
    }

    private void Exit()
    {
        _vs.Visible = false;
    }
}
