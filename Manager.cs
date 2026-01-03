using SCENeo;
using SCENeo.Ui;
using SCEWin;
using System.Collections.Frozen;

namespace SifEditor;

internal sealed partial class Manager : IRenderSource
{
    private static readonly ListBox.Option BrushTemplate = new()
    {
        UnselectedBgColor = SCEColor.Transparent,
        FitToLength = true,
    };

    private static readonly ListBox.Option MenuTemplate = new()
    {
        SelectedBgColor = SCEColor.Gray,
        Anchor = Anchor.Center,
        FitToLength = true,
    };

    private readonly Dictionary<ConsoleKey, InputMap> KeyMappings;

    private readonly Updater _updater;

    private readonly Viewport _viewport;
    private readonly Display _display;

    // render source
    private readonly Alert _alert;
    private readonly Canvas _canvas;
    private readonly TextPrompt _textPrompt;
    private readonly OptionPrompt _optionPrompt;
    private readonly KeyMapper _keyMapper;

    private readonly RenderManager _renderManager = [];

    // renderable
    private readonly TextLabel _fpsUI;
    private readonly ListBox _brushSelector;
    private readonly TextLabel _export;

    public Manager()
    {
        _updater = new Updater()
        {
            OnUpdate = Update,
            FrameCap = 60,
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

        _keyMapper = new KeyMapper();

        _fpsUI = new TextLabel()
        {
            Height = 1,
            BasePixel = Pixel.Null,
        };

        _brushSelector = new ListBox()
        {
            Width = 11,
            Height = 17,
            Offset = new Vec2I(0, 1),
            Anchor = Anchor.Right,
            BasePixel = Pixel.DarkGray,
            StackMode = StackMode.TopDown,
            Options = [..BrushTemplate.SubOption(Enumerable.Range(0, 17).Select(i => ((SCEColor)i).ToString()))]
        };

        _export = new TextLabel()
        {
            Visible = false,
            TextWrapping = TextLabel.Wrapping.Character,
        };

        _renderManager = new RenderManager()
        {
            Sources = [_canvas, this, _textPrompt, _alert, _optionPrompt, _keyMapper],
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
        _display.OnResize += _keyMapper.Display_OnResize;

        KeyMappings = new()
        {
            { ConsoleKey.Escape, new InputMap() {
                    Name   = "Back",
                    Action = () => _export.Visible = false,
                } },
            { ConsoleKey.K, new InputMap() {
                    Name = "Open keymaps",
                    Action = _keyMapper.Open,
                } },
            
            // prompts
            { ConsoleKey.O, new InputMap() {
                    Name   = "Export",
                    Action = ExportPrompt,
                } },
            { ConsoleKey.I, new InputMap() {
                    Name   = "Import",
                    Action = ImportPrompt,
                } },
            { ConsoleKey.R, new InputMap() {
                    Name   = "Resize",
                    Action = ResizePrompt,
                } },

            // paint controls
            { ConsoleKey.P, new InputMap() {
                    Name   = "Pick",
                    Action = _canvas.Pick,
                } },
            { ConsoleKey.Spacebar, new InputMap() {
                    Name   = "Paint",
                    Action = _canvas.Paint,
                } },
            { ConsoleKey.F, new InputMap() {
                    Name   = "Flood fill",
                    Action = _canvas.FloodFill,
                } },
            { ConsoleKey.T, new InputMap() {
                    Name   = "Switch paint mode",
                    Action = _canvas.NextPaintMode,
                } },

            // move
            { ConsoleKey.W, new InputMap() {
                    Name   = "Move camera up",
                    Action = () => _canvas.MoveCamera(Vec2I.Up),
                } },
            { ConsoleKey.S, new InputMap() {
                    Name   = "Move camera down",
                    Action = () => _canvas.MoveCamera(Vec2I.Left * 2),
                } },
            { ConsoleKey.A, new InputMap() {
                    Name   = "Move camera left",
                    Action = () => _canvas.MoveCamera(Vec2I.Right * 2),
                } },
            { ConsoleKey.D, new InputMap() {
                    Name   = "Move camera right",
                    Action = () => _canvas.MoveCamera(Vec2I.Up),
                } },
            { ConsoleKey.Q, new InputMap() {
                    Name   = "Move camera small left",
                    Action = () => _canvas.MoveCamera(Vec2I.Left),
                } },
            { ConsoleKey.E, new InputMap() {
                    Name   = "Move camera small right",
                    Action = () => _canvas.MoveCamera(Vec2I.Right),
                } },

            // select
            { ConsoleKey.Enter, new InputMap() {
                    Name   = "Select brush",
                    Action = SelectBrush,
                } },
            { ConsoleKey.UpArrow, new InputMap() {
                    Name   = "Move brush selector up",
                    Action = () => MoveSelector(-1),
                } },
            { ConsoleKey.DownArrow, new InputMap() {
                    Name   = "Move brush selector down",
                    Action = () => MoveSelector(+1),
                } },
            { ConsoleKey.Tab, new InputMap() {
                    Name   = "Hide brush selector",
                    Action = () => _brushSelector.Visible = !_brushSelector.Visible,
                } },
            { ConsoleKey.B, new InputMap() {
                    Name   = "Set background to hovered brush",
                    Action = () => _viewport.BasePixel = HoveredBrush(),
                } },

            // function

            { ConsoleKey.F1, new InputMap() {
                    Name   = "Toggle framecap",
                    Action = ToggleFramecap,
                } },
        };

        _keyMapper.KeyMappings = KeyMappings;
    }

    public IEnumerable<IRenderable> Render()
    {
        return [_fpsUI, _brushSelector, _export];
    }

    public void Start()
    {
        _display.Update();

        _canvas.Start();

        _updater.Start();
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
        if (_keyMapper.Visible)
        {
            LoadInput(_keyMapper.OnInput);
            return;
        }

        if (_optionPrompt.Visible)
        {
            LoadInput(_optionPrompt.OnInput);
            return;
        }

        if (_textPrompt.Visible)
        {
            LoadInput(_textPrompt.OnInput);
            return;
        }

        _canvas.FastMove = Input.Capitalize();

        LoadInput(OnInput);

        _canvas.Update(delta);
    }

    private void LoadInput(Action<ConsoleKeyInfo> action)
    {
        while (Console.KeyAvailable)
        {
            action.Invoke(Console.ReadKey(true));
        }
    }

    private void OnInput(ConsoleKeyInfo cki)
    {
        if (KeyMappings.TryGetValue(cki.Key, out InputMap? inputMap))
        {
            inputMap.Action?.Invoke();
        }
    }

    private void MoveSelector(int move)
    {
        if (_brushSelector.Visible)
        {
            _brushSelector.WrapMove(move);
        }
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

    private void ResizePrompt()
    {
        _optionPrompt.Open([.. MenuTemplate.SubOption(["Resize Current", "Clean Resize"])], result =>
        {
            switch (result)
            {
            case 0: Resize(false);
                break;
            case 1: Resize(true);
                break;
            }
        });
    }

    private void Resize(bool clean)
    {
        _textPrompt.Open("Resize - Enter width: ", TextPrompt.Result.Integer, value =>
        {
            int width = (int)value;

            _textPrompt.Open("Resize - Enter height: ", TextPrompt.Result.Integer, value =>
            {
                int height = (int)value;

                if (clean)
                {
                    _canvas.CleanResize(width, height);
                    return;
                }

                _canvas.Resize(width, height);
            });
        });
    }

    private void ImportPrompt()
    {
        _optionPrompt.Open([.. MenuTemplate.SubOption(["Import SIF", "Import from file"])], selected =>
        {
            switch (selected)
            {
            case 0: ImportSif();
                break;
            case 1: ImportFile();
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

    private void ExportPrompt()
    {
        if (_export.Visible)
        {
            return;
        }

        _optionPrompt.Open([.. MenuTemplate.SubOption(["Export here", "Export to file"])], selected =>
        {
            switch (selected)
            {
            case 0: ExportHere();
                break;
            case 1: ExportFile();
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

    private void ToggleFramecap()
    {
        _updater.FrameCap = _updater.FrameCap == Updater.Uncapped ? 60 : Updater.Uncapped;
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