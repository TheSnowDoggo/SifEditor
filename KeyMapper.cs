using SCENeo;
using SCENeo.Ui;

namespace SifEditor;

internal sealed class KeyMapper : IRenderSource
{
    private sealed class Option : ListBox.Option
    {
        public ConsoleKey Key { get; set; }
    }

    private static ListBox.Option Template = new()
    {
        FitToLength = true,
    };

    private readonly ListBox _listBox;

    public KeyMapper()
    {
        _listBox = new ListBox()
        {
            Width = 60,
            Anchor = Anchor.Center,
            Visible = false,
        };
    }

    public Dictionary<ConsoleKey, InputMap> KeyMappings { get; set; } = null!;

    public bool Visible => _listBox.Visible;

    public void Display_OnResize(int width, int height)
    {
        _listBox.Height = height;
    }

    public IEnumerable<IRenderable> Render()
    {
        return [_listBox];
    }

    public void OnInput(ConsoleKeyInfo cki)
    {
        switch (cki.Key)
        {
        case ConsoleKey.Escape:
            _listBox.Visible = false;

            _listBox.Options = null!;
            break;
        case ConsoleKey.UpArrow:
            _listBox.ScrollMove(-1);
            break;
        case ConsoleKey.DownArrow:
            _listBox.ScrollMove(+1);
            break;
        }
    }

    public void Open()
    {
        UpdateListBox();

        _listBox.Visible = true;
    }

    private void UpdateListBox()
    {
        _listBox.Options = new UpdateList<ListBox.Option>(KeyMappings.Count);

        int longestKey = KeyMappings.Keys.Max(key => key.ToString().Length);

        foreach ((ConsoleKey key, InputMap inputMap) in KeyMappings)
        {
            var option = new Option()
            {
                Key = key,
                Inhertited = Template,
                Text = $"{key.ToString().PadRight(longestKey)} - {inputMap.Name}",
            };

            _listBox.Options.Add(option);
        }
    }
}