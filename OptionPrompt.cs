using SCENeo;
using SCENeo.Ui;

namespace SifEditor;

internal sealed class OptionPrompt : IRenderSource
{
    private readonly ListBox _listBox;

    private Action<int>? _callback = null;

    public OptionPrompt()
    {
        _listBox = new ListBox()
        {
            Width = 30,
            Anchor = SCENeo.Anchor.Center | SCENeo.Anchor.Middle,
            Visible = false,
        };
    }

    public bool Visible { get { return _listBox.Visible; } }

    public IEnumerable<IRenderable> Render()
    {
        return [_listBox];
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
            _callback?.Invoke(_listBox.Selected);
            break;
        case ConsoleKey.UpArrow:
            _listBox.WrapMove(-1);
            break;
        case ConsoleKey.DownArrow:
            _listBox.WrapMove(+1);
            break;
        }
    }

    public void Open(UpdateList<ListBox.Option> options, Action<int> callback)
    {
        _listBox.Options = options;
        _listBox.Selected = 0;
        _listBox.Height = options.Count;
        _listBox.Visible = true;

        _callback = callback;
    }

    private void Exit()
    {
        _listBox.Visible = false;
    }
}
