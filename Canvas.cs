using SCENeo;
using SCENeo.Ui;

namespace SifEditor;

internal sealed class Canvas : IRenderSource
{
    public enum PaintMode
    {
        Wide,
        Left,
        Right,
    }

    private static readonly IReadOnlyCollection<Vec2I> FloodFillAxis =
    [
        Vec2I.Up, Vec2I.Down, Vec2I.Left, Vec2I.Right,
    ];

    private DisplayMap _data;
    private readonly DisplayMap _cursor;
    private readonly TextBox _brushVisual;

    private PaintMode _mode = PaintMode.Wide;

    public Canvas()
    {
        _data = new DisplayMap(80, 40);

        _data.Fill(Pixel.White);

        _cursor = new DisplayMap(2, 1);

        _brushVisual = new TextBox()
        {
            Width = 19,
            Height = 1,
            TextBgColor = SCEColor.Transparent,
            Anchor = Anchor.Right,
        };

        UpdateBrushVisual();
    }

    private Pixel _brush;

    public Pixel Brush
    {
        get { return _brush; }
        set
        {
            if (value == _brush)
            {
                return;
            }

            _brush = value;

            UpdateBrushVisual();
        }
    }

    public void SetCursorPosition(Vec2I position)
    {
        _cursor.Offset = position;

        UpdateCursor();
    }

    public IEnumerable<IRenderable> Render()
    {
        return [_data, _cursor, _brushVisual];
    }

    private Vec2I GetCursorCanvasPosition()
    {
        return _cursor.Offset - _data.Offset;
    }

    public void SetOffset(Vec2I offset)
    {
        _data.Offset = offset;

        UpdateCursor();
    }

    public void Pick()
    {
        Vec2I pos = GetCursorCanvasPosition();

        if (_mode == PaintMode.Right)
        {
            pos += Vec2I.Right;
        }

        if (!_data.InRange(pos))
        {
            return;
        }

        Brush = _data[pos];
    }

    public void Paint()
    {
        Vec2I pos = GetCursorCanvasPosition();

        switch (_mode)
        {
        case PaintMode.Wide:
            if (_data.InRange(pos))
            {
                _data[pos] = Brush;
            }

            pos += Vec2I.Right;

            if (_data.InRange(pos))
            {
                _data[pos] = Brush;
            }
            break;
        case PaintMode.Left:
            if (_data.InRange(pos))
            {
                _data[pos] = Brush;
            }
            break;
        case PaintMode.Right:
            pos += Vec2I.Right;

            if (_data.InRange(pos))
            {
                _data[pos] = Brush;
            }
            break;
        }

        UpdateCursor();
    }

    public void FloodFill()
    {
        Vec2I pos = GetCursorCanvasPosition();

        if (!_data.InRange(pos))
        {
            return;
        }

        SCEColor fill = _data[pos].BgColor;

        if (fill == Brush.BgColor)
        {
            return;
        }

        var stack = new Stack<Vec2I>();

        stack.Push(pos);

        while (stack.TryPop(out Vec2I curPos))
        {
            _data[curPos] = Brush;

            foreach (Vec2I axis in FloodFillAxis)
            {
                Vec2I nextPos = curPos + axis;

                if (!_data.InRange(nextPos))
                {
                    continue;
                }

                if (_data[nextPos].BgColor != fill)
                {
                    continue;
                }

                stack.Push(nextPos);
            }
        }

        UpdateCursor();
    }

    public void NextMode()
    {
        _mode = (PaintMode)(((int)_mode + 1) % 3);

        UpdateCursor();
    }

    public void Import(Grid2D<Pixel> grid)
    {
        Vec2I offset = _data.Offset;

        _data = new DisplayMap(grid)
        {
            Offset = offset
        };
    }

    public string Export()
    {
        return SIFUtils.Serialize(_data);
    }

    private void UpdateBrushVisual()
    {
        _brushVisual.Text = $"Brush - {Brush.BgColor}";
        _brushVisual.BasePixel = Brush;
        _brushVisual.TextFgColor = Brush.BgColor.Contrast();
    }

    private void UpdateCursor()
    {
        Vec2I pos = GetCursorCanvasPosition();

        if (_data.InRange(pos))
        {
            _cursor[0, 0] = new Pixel(CursorLeft(), _data[pos].BgColor.Contrast(), SCEColor.Transparent);
        }

        pos += Vec2I.Right;

        if (_data.InRange(pos))
        {
            _cursor[1, 0] = new Pixel(CursorRight(), _data[pos].BgColor.Contrast(), SCEColor.Transparent);
        }
    }

    private char CursorLeft()
    {
        return _mode is PaintMode.Wide or PaintMode.Left ? '>' : '\0';
    }

    private char CursorRight()
    {
        return _mode is PaintMode.Wide or PaintMode.Right ? '<' : '\0';
    }
}