using SCENeo;
using SCENeo.Ui;
using SCEWin;

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
            FrameCap = Program.Config.StartFrameCapped ? Program.Config.LockedFrameCap : Updater.Uncapped,
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
            Visible = Program.Config.ShowFPS,
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
            BasePixel = new Pixel(Program.Config.DefaultBackgroundColor),
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
                    Action = () => _canvas.MoveCamera(Vec2I.Down),
                } },
            { ConsoleKey.A, new InputMap() {
                    Name   = "Move camera left",
                    Action = () => _canvas.MoveCamera(Vec2I.Left * 2),
                } },
            { ConsoleKey.D, new InputMap() {
                    Name   = "Move camera right",
                    Action = () => _canvas.MoveCamera(Vec2I.Right * 2),
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
                    Action = SetBackground,
                } },

            // function

            { ConsoleKey.F1, new InputMap() {
                    Name   = "Toggle framecap",
                    Action = ToggleFramecap,
                } },
        };

        _keyMapper.KeyMappings = KeyMappings;
    }

    public string? InitialFile { get; set; }

    public IEnumerable<IRenderable> Render()
    {
        return [_fpsUI, _brushSelector, _export];
    }

    public void Start()
    {
        _display.Update();

        _canvas.Start();

        ImportInitialFile();

        _updater.Start();
    }

    private static void LoadInput(Action<ConsoleKeyInfo> action)
    {
        while (Console.KeyAvailable)
        {
            action.Invoke(Console.ReadKey(true));
        }
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
        _optionPrompt.Open([.. MenuTemplate.SubOption(["Resize Current", "Clean Resize", "Reset to Defualt"])], result =>
        {
            switch (result)
            {
            case 0: 
                Resize(false);
                break;
            case 1: 
                Resize(true);
                break;
            case 2:
                _optionPrompt.Open([.. MenuTemplate.SubOption(["No", "Yes, I may lose data."])], result =>
                {
                    if (result == 1)
                    {
                        _canvas.ResetToDefault();
                    }
                });
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
        _optionPrompt.Open([.. MenuTemplate.SubOption(["Import SIF here", "Import BINIMG file", "Import SIF file"])], selected =>
        {
            switch (selected)
            {
            case 0:
                _textPrompt.Open("Enter SIF: ", sif => _canvas.ImportSif((string)sif));
                break;
            case 1:
                _textPrompt.Open("Enter BINIMG filepath: ", filepath => _canvas.ImportImgFile((string)filepath));
                break;
            case 2:
                _textPrompt.Open("Enter SIF filepath: ", filepath => _canvas.ImportSifFile((string)filepath));
                break;
            }
        });
    }

    private void ExportPrompt()
    {
        if (_export.Visible)
        {
            return;
        }

        _optionPrompt.Open([.. MenuTemplate.SubOption(["Export", "Show SIF", "Export BINIMG file", "Export SIF file"])],
        selected =>
        {
            switch (selected)
            {
            case 0:
                _canvas.ExportDefault();
                break;
            case 1:
                _export.Visible = true;
                _export.Text = _canvas.ExportSif();
                break;
            case 2:
                _optionPrompt.Open([.. MenuTemplate.SubOption(["Full", "Opaque", "BgOnly", "BgOnlyOpaque"])], 
                selected =>
                {
                    _textPrompt.Open("Enter file path: ", "", ".binimg", 
                        filepath => _canvas.ExportToImgFile((string)filepath, (ImageSerializer.Mode)selected));
                });
                break;
            case 3:
                _textPrompt.Open("Enter file path: ", "", ".sif", 
                    filepath => _canvas.ExportToSifFile((string)filepath));
                break;
            }
        });
    }

    private void ImportInitialFile()
    {
        if (InitialFile == null)
        {
            return;
        }

        string extension = Path.GetExtension(InitialFile).ToLower();

        switch (extension)
        {
        case ".binimg":
            _canvas.ImportImgFile(InitialFile);
            break;
        case ".sif":
            _canvas.ImportSifFile(InitialFile);
            break;
        default:
            _alert.Show($"Unsupported file extention {extension}");
            break;
        }
    }

    private void ToggleFramecap()
    {
        _updater.FrameCap = _updater.FrameCap == Updater.Uncapped ? Program.Config.LockedFrameCap : Updater.Uncapped;
    }

    private void SetBackground()
    {
        _viewport.BasePixel = HoveredBrush();

        _canvas.Background = _viewport.BasePixel.BgColor;
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