using SCENeo;
using SCENeo.Ui;
using SCEWin;

namespace SifEditor;

internal sealed class Manager : IRenderSource
{
    private static readonly VerticalSelector.Option OptionTemplate = new()
    {
        Anchor = Anchor.Center,
        FitToLength = true,
    };

    private readonly Updater _updater;

    private readonly Viewport _viewport;
    private readonly Display _display;

    // render source
    private readonly Alert _alert;
    private readonly Canvas _canvas;
    private readonly TextPrompt _textPrompt;
    private readonly OptionPrompt _optionPrompt;

    private readonly RenderManager _renderManager = [];

    // renderable
    private readonly TextLabel _fpsUI;
    private readonly VerticalSelector _brushSelector;
    private readonly TextLabel _export;

    public Manager()
    {
        _updater = new Updater()
        {
            OnUpdate = Update,
        };

        _alert = new Alert();

        _canvas = new Canvas()
        {
            Alert = _alert,
        };

        _textPrompt = new TextPrompt()
        {
            Alert = _alert,
        };

        _optionPrompt = new OptionPrompt();

        _fpsUI = new TextLabel()
        {
            Height = 1,
            BasePixel = Pixel.Null,
        };

        _brushSelector = new VerticalSelector()
        {
            Width = 11,
            Height = 17,
            Offset = new Vec2I(0, 1),
            Anchor = Anchor.Right,
            BasePixel = Pixel.DarkGray,
            StackMode = StackMode.TopDown,
        };

        for (int i = 0; i < 17; i++)
        {
            SCEColor color = (SCEColor)i;

            var option = new VerticalSelector.Option()
            {
                Text = color.ToString(),
                UnselectedBgColor = SCEColor.Transparent,
                FitToLength = true,
            };

            _brushSelector.Options.Add(option);
        }

        _export = new TextLabel()
        {
            Visible = false,
            TextWrapping = TextLabel.Wrapping.Character,
        };

        _renderManager = new RenderManager()
        {
            Sources = [_canvas, this, _textPrompt, _alert, _optionPrompt],
        };

        _viewport = new Viewport()
        {
            BasePixel = Pixel.DarkCyan,
            Source = _renderManager,
        };

        _display = new Display()
        {
            Renderable = _viewport,
            Output = WinOutput.Instance,
        };

        _display.OnResize += Display_OnResize;
        _display.OnResize += _canvas.Display_OnResize;
    }

    public void Run()
    {
        _updater.Start();
    }

    public IEnumerable<IRenderable> Render()
    {
        return [_fpsUI, _brushSelector, _export];
    }

    private void Update(double delta)
    {
        UpdateInput(delta);

        _fpsUI.Text = $"FPS: {_updater.FPS:0}";

        _textPrompt.Update(delta);
        _alert.Update(delta);

        _display.Update();
    }

    private void UpdateInput(double delta)
    {
        if (_optionPrompt.Visible)
        {
            while (Console.KeyAvailable)
            {
                _optionPrompt.OnInput(Console.ReadKey(true));
            }

            return;
        }

        if (_textPrompt.Visible)
        {
            while (Console.KeyAvailable)
            {
                _textPrompt.OnInput(Console.ReadKey(true));
            }

            return;
        }

        _canvas.FastMove = Input.Capitalize();

        while (Console.KeyAvailable)
        {
            OnInput(Console.ReadKey(true));
        }

        _canvas.Update(delta);
    }

    private void OnInput(ConsoleKeyInfo cki)
    {
        switch (cki.Key)
        {
        case ConsoleKey.Escape:
            _export.Visible = false;
            break;
        case ConsoleKey.P:
            _canvas.Pick();
            break;
        case ConsoleKey.O:
            Export();
            break;
        case ConsoleKey.I:
            Import();
            break;
        case ConsoleKey.Spacebar:
            _canvas.Paint();
            break;
        case ConsoleKey.F:
            _canvas.FloodFill();
            break;
        case ConsoleKey.Tab:
            _brushSelector.Visible = !_brushSelector.Visible;
            break;
        case ConsoleKey.B:
            _viewport.BasePixel = HoveredBrush();
            break;
        case ConsoleKey.R:
            ResizePrompt();
            break;
        case ConsoleKey.T:
            _canvas.NextPaintMode();
            break;

        // function
        case ConsoleKey.F1:
            _updater.FrameCap = _updater.FrameCap == Updater.Uncapped ? 60 : Updater.Uncapped;
            break;

        // move
        case ConsoleKey.W:
            _canvas.MoveCamera(Vec2I.Up);
            break;
        case ConsoleKey.S:
            _canvas.MoveCamera(Vec2I.Down);
            break;
        case ConsoleKey.A:
            _canvas.MoveCamera(Vec2I.Left * 2);
            break;
        case ConsoleKey.D:
            _canvas.MoveCamera(Vec2I.Right * 2);
            break;
        case ConsoleKey.Q:
            _canvas.MoveCamera(Vec2I.Left);
            break;
        case ConsoleKey.E:
            _canvas.MoveCamera(Vec2I.Right);
            break;

        // select
        case ConsoleKey.Enter:
            SelectBrush();
            break;
        case ConsoleKey.UpArrow:
            MoveSelector(-1);
            break;
        case ConsoleKey.DownArrow:
            MoveSelector(+1);
            break;
        }
    }

    private void MoveSelector(int move)
    {
        if (_brushSelector.Visible)
        {
            _brushSelector.WrapMove(move);
        }
    }

    private void ResizePrompt()
    {
        _textPrompt.Open("Resize - Enter width: ", TextPrompt.Result.Integer, value =>
        {
            int width = (int)value;

            _textPrompt.Open("Resize - Enter height: ", TextPrompt.Result.Integer,  value =>
            {
                int height = (int)value;

                _canvas.Resize(width, height);
            });
        });
    }

    private Pixel HoveredBrush()
    {
        return new Pixel((SCEColor)_brushSelector.Selected);
    }
 
    private void SelectBrush()
    {
        if (_brushSelector.Visible)
        {
            _canvas.Brush = HoveredBrush();
        }
    }

    private void Import()
    {
        _optionPrompt.Open(OptionTemplate.FromArray("Import SIF", "Import from file"), selected =>
        {
            switch (selected)
            {
            case 0:
                ImportSif();
                break;
            case 1:
                ImportFile();
                break;
            }
        });
    }

    private void ImportSif()
    {
        _textPrompt.Open("Enter SIF: ", sif =>
        {
            try
            {
                _canvas.Import(SIFUtils.Deserialize((string)sif));

                _alert.Show("Import successful!", SCEColor.Green);
            }
            catch (Exception e)
            {
                _alert.Show($"Failed to import: {e.Message}");
            }
        });
    }

    private void ImportFile()
    {
        _textPrompt.Open("Enter file path: ", filepath =>
        {
            try
            {
                _canvas.Import(ImageSerializer.Deserialize((string)filepath));

                _alert.Show("Import successful!", SCEColor.Green);
            }
            catch (Exception e)
            {
                _alert.Show($"Failed to import: {e.Message}");
            }
        });
    }

    private void Export()
    {
        if (_export.Visible)
        {
            return;
        }

        _optionPrompt.Open(OptionTemplate.FromArray("Export here", "Export to file"), selected =>
        {
            switch (selected)
            {
            case 0:
                ExportHere();
                break;
            case 1:
                ExportFile();
                break;
            }
        });
    }

    private void ExportHere()
    {
        _export.Visible = !_export.Visible;

        if (!_export.Visible)
        {
            return;
        }

        _export.Text = _canvas.ExportSif();
    }

    private void ExportFile()
    {
        _textPrompt.Open("Enter file path: ", filepath =>
        {
            _canvas.ExportToFile((string)filepath);
        });
    }

    private void Display_OnResize(int width, int height)
    {
        _viewport.Width  = width;
        _viewport.Height = height;

        _fpsUI.Width = width;

        _export.Width  = width;
        _export.Height = height;
    }
}