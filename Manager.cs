using SCENeo;
using SCENeo.Ui;
using SCEWin;

namespace SifEditor;

internal sealed class Manager : IRenderSource
{
    private const double CameraSpeed = 40;

    private readonly Updater _updater;

    private readonly Viewport _viewport;
    private readonly Display _display;

    // render source
    private readonly Canvas _canvas;
    private readonly Prompt _prompt;
    private readonly Alert _alert;

    private readonly RenderManager _renderManager = [];

    // renderable
    private readonly TextBox _fpsUI;
    private readonly VerticalSelector _brushSelector;
    private readonly TextBox _export;

    private bool _lastOrthogonal;

    private Vec2 _cameraPos;

    private bool _fastMove;

    public Manager()
    {
        _updater = new Updater()
        {
            OnUpdate = Update,
        };

        _canvas = new Canvas();

        _prompt = new Prompt();

        _alert = new Alert();

        _fpsUI = new TextBox()
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
        };

        for (int i = 0; i < 17; i++)
        {
            SCEColor color = (SCEColor)i - 1;

            _brushSelector[i] = new Option()
            {
                Text = color.ToString().PadRight(_brushSelector.Width),
                UnselectedBgColor = SCEColor.Transparent,
            };
        }

        _export = new TextBox()
        {
            Visible = false,
            TextWrapping = TextBox.Wrapping.Character,
        };

        _renderManager = new RenderManager()
        {
            Sources = [_canvas, this, _prompt, _alert],
        };

        _viewport = new Viewport()
        {
            BasePixel = Pixel.DarkCyan,
            Source = _renderManager,
        };

        _display = new Display()
        {
            Source = _viewport,
            Output = WinOutput.Instance,
            OnResize = Display_OnResize,
        };
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

        _prompt.Update(delta);
        _alert.Update(delta);

        _display.Update();
    }

    private void UpdateInput(double delta)
    {
        if (_prompt.Visible)
        {
            while (Console.KeyAvailable)
            {
                _prompt.OnInput(Console.ReadKey(true));
            }

            return;
        }

        _fastMove = Input.KeyPressed(Key.Shift) || Console.CapsLock;

        while (Console.KeyAvailable)
        {
            OnInput(Console.ReadKey(true));
        }

        MoveCanvas(delta);
    }

    private void OnInput(ConsoleKeyInfo cki)
    {
        switch (cki.Key)
        {
        case ConsoleKey.P:
            _canvas.Pick();
            break;
        case ConsoleKey.O:
            Export();
            break;
        case ConsoleKey.I:
            _prompt.Open("Enter SIF: ", ImportSif);
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
        case ConsoleKey.T:
            _canvas.NextMode();
            break;

        // function
        case ConsoleKey.F1:
            _updater.FrameCap = _updater.FrameCap == Updater.Uncapped ? 60 : Updater.Uncapped;
            break;

        // move
        case ConsoleKey.W:
            SlowMove(Vec2I.Up);
            break;
        case ConsoleKey.S:
            SlowMove(Vec2I.Down);
            break;
        case ConsoleKey.A:
            SlowMove(Vec2I.Left * 2);
            break;
        case ConsoleKey.D:
            SlowMove(Vec2I.Right * 2);
            break;
        case ConsoleKey.Q:
            SlowMove(Vec2I.Left);
            break;
        case ConsoleKey.E:
            SlowMove(Vec2I.Right);
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

    private static Vec2I MoveVector()
    {
        Vec2I move = Vec2I.Zero;

        if (Input.KeyPressed(Key.W))
            move += Vec2I.Up;
        if (Input.KeyPressed(Key.S))
            move += Vec2I.Down;
        if (Input.KeyPressed(Key.A))
            move += Vec2I.Left;
        if (Input.KeyPressed(Key.D))
            move += Vec2I.Right;

        return move;
    }

    private void MoveCanvas(double delta)
    {
        if (!_fastMove)
        {
            return;
        }

        Vec2I moveVec = MoveVector();

        if (moveVec == Vec2.Zero)
        {
            return;
        }

        bool orthogonal = moveVec.X == 0 || moveVec.Y == 0;

        // for smooth diaganol movement
        if (!orthogonal && _lastOrthogonal)
        {
            _cameraPos = _cameraPos.Round();
        }

        _lastOrthogonal = orthogonal;

        _cameraPos += ((Vec2)moveVec).Normalized() * (float)(CameraSpeed * delta);

        UpdateCanvasPosition();

        if (Input.KeyPressed(Key.Space))
        {
            _canvas.Paint();
        }
    }

    private void SlowMove(Vec2I move)
    {
        if (_fastMove)
        {
            return;
        }

        _cameraPos = _cameraPos.Round() + move;

        UpdateCanvasPosition();
    }

    private void UpdateCanvasPosition()
    {
        _canvas.SetOffset(-(Vec2I)_cameraPos.Round());
    }

    private void ImportSif(string sif)
    {
        try
        {
            _canvas.Import(SIFUtils.Deserialize(sif));
        }
        catch (Exception e)
        {
            _alert.Show($"Failed to import: {e.Message}");
        }
    }

    private Pixel HoveredBrush()
    {
        return new Pixel((SCEColor)_brushSelector.Selected - 1);
    }
 
    private void SelectBrush()
    {
        if (_brushSelector.Visible)
        {
            _canvas.Brush = HoveredBrush();
        }
    }

    private void Export()
    {
        _export.Visible = !_export.Visible;

        if (!_export.Visible)
        {
            return;
        }

        _export.Text = _canvas.Export();
    }

    private void Display_OnResize(int width, int height)
    {
        _viewport.Width  = width;
        _viewport.Height = height;

        _fpsUI.Width = width;

        _canvas.SetCursorPosition(new Vec2I(width, height) / 2);

        _export.Width  = width;
        _export.Height = height;
    }
}