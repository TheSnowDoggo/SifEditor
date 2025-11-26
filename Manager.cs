using SCE_WinInputNeo;
using SCENeo;
using SCENeo.UI;
using SCENeo.Utils;

namespace SifEditor;

internal sealed class Manager
{
    private readonly Viewport _viewport;

    private readonly Display _display;

    private readonly DisplayMap _canvas;

    public Manager()
    {
        _viewport = new Viewport()
        {
            BasePixel = new Pixel(SCEColor.DarkCyan),
        };

        _display = new Display()
        {
            Source   = _viewport,
            OnResize = Display_OnResize,
        };

        _canvas = new DisplayMap(20, 10)
        {
        };

        _canvas.Fill(new Pixel(SCEColor.White));

        _viewport.Renderables.Add(_canvas);
    }

    public void Run()
    {
        _display.Update();

        LLKeyboard.OnInput += LLKeyboard_OnInput;

        LLKeyboard.Start();
    }

    private void LLKeyboard_OnInput(MessageType type, KBDLLHookStruct kbdll)
    {
        if (type != MessageType.KeyDown)
        {
            return;
        }

        Key key = (Key)kbdll.VkCode;

        switch (key)
        {
        case Key.P:
            _viewport.BasePixel = new Pixel((SCEColor)((int)(_viewport.BasePixel.Colors.BackgroundColor + 1) % 16));
            _display.Update();
            break;
        case Key.W:
            MoveCanvas(Vec2I.Up);
            break;
        case Key.S:
            MoveCanvas(Vec2I.Down);
            break;
        case Key.A:
            MoveCanvas(Vec2I.Left);
            break;
        case Key.D: 
            MoveCanvas(Vec2I.Right);
            break;
        }
    }

    private void MoveCanvas(Vec2I move)
    {
        _canvas.Offset -= move;
        _display.Update();
    }

    private void Display_OnResize(Vec2I newSize)
    {
        _viewport.Resize(newSize);
    }
}