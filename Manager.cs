using SCENeo;
using SCENeo.UI;
using SCENeo.Utils;
using SCEWin;
using System;
using System.Collections.Concurrent;

namespace SifEditor;

internal sealed class Manager
{
    private enum PaintMode
    {
        Wide,
        Left,
        Right,
    }

    private const double CameraSpeed = 40;

    private readonly IReadOnlyCollection<Vec2I> FloodFillAxis = 
    [
        Vec2I.Up, Vec2I.Down, Vec2I.Left, Vec2I.Right,
    ];

    private readonly Thread _inputThread;

    private readonly Updater _updater;

    private readonly Viewport _viewport;
    private readonly Display _display;

    private readonly DisplayMap _canvas;

    private readonly TextBox _fpsUI;

    private readonly DisplayMap _cursor;

    private readonly TextBox _brushVisual;

    private readonly VerticalSelector _brushSelector;

    private readonly TextBox _export;

    private readonly TextBox _text;

    private readonly VirtualConsole _console = new();

    private readonly ConcurrentQueue<InputData> _inputQueue = new();

    private readonly ConsoleInputStream _inputStream = new();

    private bool _lastSingle;

    private Vec2 _cameraPos;

    private Pixel _brush = Pixel.Blue;

    private PaintMode _paintMode = PaintMode.Wide;

    private bool _entry;

    public Manager()
    {
        _inputThread = new Thread(StartInput);

        _updater = new Updater()
        {
            OnUpdate = Update,
        };

        _viewport = new Viewport()
        {
            BasePixel = new Pixel(SCEColor.DarkCyan),
        };

        _display = new Display()
        {
            Source   = _viewport,
            Output   = WinOutput.Instance,
            OnResize = Display_OnResize,
        };

        _canvas = new DisplayMap(80, 40);

        _canvas.Fill(Pixel.White);

        _canvas.Fill(Pixel.Black, new Rect2DI(0, 0, 4, 10));

        _fpsUI = new TextBox()
        {
            Height    = 1,
            BasePixel = Pixel.Transparent,
        };

        _brushVisual = new TextBox()
        {
            Width = 19,
            Height = 1,
            TextBgColor = SCEColor.Transparent,
            Anchor = Anchor.Right,
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
            Enabled      = false,
            TextWrapping = true,
        };

        _text = new TextBox()
        {
            Width = 50,
            Height = 3,
            Anchor = Anchor.Center | Anchor.Middle,
            TextWrapping = true,
        };

        _console = new VirtualConsole()
        {
            Width = 30,
            Height = 15,
            BufferWidth = 30,
            BufferHeight = 9000,
            Autoscroll = true,
            Offset = new Vec2I(0, 1),
        };

        _cursor = new DisplayMap(2, 1);

        _viewport.Renderables.AddEvery(_canvas, _fpsUI, _cursor, _brushVisual, _brushSelector, _export, _text, _console);
    }

    public void Run()
    {
        Start();

        _inputThread.Start();

        _updater.Start();
    }

    private void Start()
    {
        UpdateCursor();

        UpdateBrushVisual();
    }

    private void Update(double delta)
    {
        UpdateInput(delta);

        _fpsUI.Text = $"FPS: {_updater.FPS}";

        _display.Update();
    }

    private void UpdateInput(double delta)
    {
        if (_entry)
        {
            while (Console.KeyAvailable)
            {
                OnEntryKey(Console.ReadKey(true));
            }

            return;
        }

        while (_inputQueue.TryDequeue(out InputData? inputData))
        {
            OnInput(inputData);
        }

        MoveCanvas(delta);
    }

    private void OnEntryKey(ConsoleKeyInfo cki)
    {
        _inputStream.Next(cki);

        _text.Text = _inputStream.ToString();
    }

    private void StartInput()
    {
        WinKeyboard.OnInput += LLKeyboard_OnInput;

        WinKeyboard.Start();
    }

    private void OnInput(InputData inputData)
    {
        return;

        if (inputData.Type != MessageType.KeyDown)
        {
            return;
        }

        switch (inputData.Kbdll.Key)
        {
        case Key.P:
            Pick();
            break;
        case Key.O:
            Export();
            break;
        case Key.Space:
            Paint();
            break;
        case Key.F:
            FloodFill();
            break;
        case Key.Tab:
            _brushSelector.Enabled = !_brushSelector.Enabled;
            break;

        case Key.W:
            SlowMove(Vec2I.Up);
            break;
        case Key.S:
            SlowMove(Vec2I.Down);
            break;
        case Key.A:
            SlowMove(Vec2I.Left * 2);
            break;
        case Key.D:
            SlowMove(Vec2I.Right * 2);
            break;
        case Key.Q:
            SlowMove(Vec2I.Left);
            break;
        case Key.E:
            SlowMove(Vec2I.Right);
            break;

        case Key.Enter:
            SelectBrush();
            break;
        case Key.Up:
            MoveSelector(-1);
            break;
        case Key.Down:
            MoveSelector(+1);
            break;
        case Key.V:
            break;
        }
    }

    private void MoveSelector(int move)
    {
        if (_brushSelector.Enabled)
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
        if (!Input.KeyPressed(Key.Shift))
        {
            return;
        }

        Vec2I moveVec = MoveVector();

        if (moveVec == Vec2.Zero)
        {
            return;
        }

        bool single = moveVec.X == 0 || moveVec.Y == 0;

        // for smooth diaganol movement
        if (!single && _lastSingle)
        {
            _cameraPos = _cameraPos.Round();
        }

        _lastSingle = single;

        _cameraPos += ((Vec2)moveVec).Normalized() * (float)(CameraSpeed * delta);

        UpdateCanvasPosition();
    }

    private void SlowMove(Vec2I move)
    {
        if (Input.KeyPressed(Key.Shift))
        {
            return;
        }

        _cameraPos = _cameraPos.Round() + move;

        UpdateCanvasPosition();
    }

    private void UpdateCanvasPosition()
    {
        _canvas.Offset = -(Vec2I)_cameraPos.Round();

        UpdateCursor();
    }
 
    private void UpdateCursor()
    {
        Vec2I pos = GetCursorCanvasPosition();

        if (_canvas.InRange(pos))
        {
            _cursor[0, 0] = new Pixel('>', _canvas[pos].BgColor.Contrast(), SCEColor.Transparent);
        }

        pos += Vec2I.Right;

        if (_canvas.InRange(pos))
        {
            _cursor[1, 0] = new Pixel('<', _canvas[pos].BgColor.Contrast(), SCEColor.Transparent);
        }
    }

    private Vec2I GetCursorCanvasPosition()
    {
        return _cursor.Offset - _canvas.Offset;
    }

    private void Pick()
    {
        Vec2I pos = GetCursorCanvasPosition();

        if (_canvas.InRange(pos))
        {
            _brush = _canvas[pos];
            UpdateBrushVisual();
        }
    }

    private void Paint()
    {
        Vec2I pos = GetCursorCanvasPosition();
        
        switch (_paintMode)
        {
        case PaintMode.Wide:
            if (_canvas.InRange(pos))
            {
                _canvas[pos] = _brush;
            }

            pos += Vec2I.Right;

            if (_canvas.InRange(pos))
            {
                _canvas[pos] = _brush;
            }
            break;
        case PaintMode.Left:
            if (_canvas.InRange(pos))
            {
                _canvas[pos] = _brush;
            }
            break;
        case PaintMode.Right:
            pos += Vec2I.Right;

            if (_canvas.InRange(pos))
            {
                _canvas[pos] = _brush;
            }
            break;
        }
    }

    private void FloodFill()
    {
        Vec2I pos = GetCursorCanvasPosition();

        if (!_canvas.InRange(pos))
        {
            return;
        }

        SCEColor fill = _canvas[pos].BgColor;

        Pixel brush = _brush;

        if (fill == brush.BgColor)
        {
            return;
        }

        var stack = new Stack<Vec2I>();

        stack.Push(pos);

        while (stack.TryPop(out Vec2I curPos))
        {
            _canvas[curPos] = brush;

            foreach (Vec2I axis in FloodFillAxis)
            {
                Vec2I nextPos = curPos + axis;

                if (!_canvas.InRange(nextPos))
                {
                    continue;
                }

                if (_canvas[nextPos].BgColor != fill)
                {
                    continue;
                }

                stack.Push(nextPos);
            }
        }
    }

    private void UpdateBrushVisual()
    {
        _brushVisual.Text        = $"Brush - {_brush.BgColor}";
        _brushVisual.BasePixel   = _brush;
        _brushVisual.TextFgColor = _brush.BgColor.Contrast();
    }

    private void SelectBrush()
    {
        if (_brushSelector.Enabled)
        {
            _brush = new Pixel((SCEColor)_brushSelector.Selected - 1);
            UpdateBrushVisual();
        }
    }

    private void Export()
    {
        _export.Enabled = !_export.Enabled;

        if (!_export.Enabled)
        {
            return;
        }

        _export.Text = SIFUtils.Serialize(_canvas);
    }

    private void Display_OnResize(Vec2I newSize)
    {
        _viewport.Width = newSize.X;
        _viewport.Height = newSize.Y;

        _fpsUI.Width = newSize.X;

        _cursor.Offset = newSize / 2;

        _export.Width = newSize.X;
        _export.Height = newSize.Y;
    }

    private void LLKeyboard_OnInput(MessageType type, KBDLLHookStruct kbdll)
    {
        _inputQueue.Enqueue(new InputData(type, kbdll));
    }
}