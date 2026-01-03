using SCENeo;
using SCENeo.Ui;
using SCEWin;

namespace SifEditor;

internal sealed class Canvas : IRenderSource
{
    public enum PaintMode
    {
        Wide,
        Left,
        Right,
    }

    private const double CameraSpeed = 40;

    private static readonly IReadOnlyCollection<Vec2I> FloodFillAxis =
    [
        Vec2I.Up, Vec2I.Down, Vec2I.Left, Vec2I.Right,
    ];

    private DisplayMap _data;
    private readonly DisplayMap _cursor;
    private readonly TextLabel _brushVisual;

    private PaintMode _mode = PaintMode.Wide;

    private bool _lastOrthogonal;

    private Vec2 _cameraPos;

    private Vec2I _displaySize;

    public Canvas()
    {
        _data = new DisplayMap(Image.Plain(20, 10, Pixel.White));

        _cursor = new DisplayMap(2, 1);

        _brushVisual = new TextLabel()
        {
            Width = 19,
            Height = 1,
            TextBgColor = SCEColor.Transparent,
            Anchor = Anchor.Right,
        };
    }

    public Alert? Alert { get; set; }

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

    public bool FastMove { get; set; }

    public IEnumerable<IRenderable> Render()
    {
        return [_data, _cursor, _brushVisual];
    }

    public void Start()
    {
        CenterCameraOnImage();
        UpdateBrushVisual();
        UpdateCursor();
    }

    public void Update(double delta)
    {
        if (!FastMove)
        {
            return;
        }

        Vec2I moveVec = Input.RawWASDMoveVector();

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
            Paint();
        }
    }

    public void Display_OnResize(int width, int height)
    {
        _displaySize = new Vec2I(width, height);

        _cursor.Offset = _displaySize / 2;

        UpdateCursor();
    }

    public void MoveCamera(Vec2I move)
    {
        if (FastMove)
        {
            return;
        }

        _cameraPos = _cameraPos.Round() + move;

        UpdateCanvasPosition();
    }
    
    public void Pick()
    {
        Vec2I pos = CursorCanvasPosition();

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
        Vec2I pos = CursorCanvasPosition();

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
        Vec2I pos = CursorCanvasPosition();

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

    public void NextPaintMode()
    {
        _mode = (PaintMode)(((int)_mode + 1) % 3);

        UpdateCursor();
    }

    public void Import(Grid2D<Pixel> grid)
    {
        _data = new DisplayMap(grid);

        CenterCameraOnImage();
    }

    public void ImportSif(string sif)
    {
        try
        {
            Import(SIFSerializer.DeserializeString(sif));

            Alert?.Show("Import successful!", SCEColor.Green);
        }
        catch (Exception e)
        {
            Alert?.Show($"Failed to import: {e.Message}");
        }
    }

    public void ImportImgFile(string filepath)
    {
        try
        {
            Import(ImageSerializer.Deserialize(filepath));

            Alert?.Show("Import successful!", SCEColor.Green);
        }
        catch (Exception e)
        {
            Alert?.Show($"Failed to import: {e.Message}");
        }
    }

    public void ImportSifFile(string filepath)
    {
        try
        {
            Import(SIFSerializer.Deserialize(filepath));

            Alert?.Show("Import successful!", SCEColor.Green);
        }
        catch (Exception e)
        {
            Alert?.Show($"Failed to import: {e.Message}");
        }
    }

    public void Resize(int width, int height)
    {
        _data.Resize(width, height);
        CenterCameraOnImage();
    }

    public void CleanResize(int width, int height)
    {
        _data = new DisplayMap(Image.Plain(width, height, Pixel.White));
        CenterCameraOnImage();
    }

    public string ExportSif()
    {
        return SIFSerializer.Serialize(_data);
    }

    public void ExportToImgFile(string filepath, bool opaque = false)
    {
        try
        {
            ImageSerializer.Serialize(filepath, _data, opaque);

            Alert?.Show($"Saved to {filepath} successfully!", SCEColor.Green);
        }
        catch (Exception e)
        {
            Alert?.Show(e.Message);
        }
    }

    public void ExportToSifFile(string filepath)
    {
        try
        {
            File.WriteAllText(filepath, SIFSerializer.Serialize(_data));

            Alert?.Show($"Saved to {filepath} successfully!", SCEColor.Green);
        }
        catch (Exception e)
        {
            Alert?.Show(e.Message);
        }
    }

    private void CenterCameraOnImage()
    {
        _cameraPos = (_data.Size()  - _displaySize) / 2;
        UpdateCanvasPosition();
    }

    private Vec2I CursorCanvasPosition()
    {
        return _cursor.Offset - _data.Offset;
    }

    private void UpdateBrushVisual()
    {
        _brushVisual.Text = $"Brush - {Brush.BgColor}";
        _brushVisual.BasePixel = Brush;
        _brushVisual.TextFgColor = Brush.BgColor.Contrast();
    }

    private void UpdateCursor()
    {
        Vec2I pos = CursorCanvasPosition();

        if (_data.InRange(pos))
        {
            char c = _mode is PaintMode.Wide or PaintMode.Left ? '>' : '\0';

            _cursor[0, 0] = new Pixel(c, _data[pos].BgColor.Contrast(), SCEColor.Transparent);
        }

        pos += Vec2I.Right;

        if (_data.InRange(pos))
        {
            char c = _mode is PaintMode.Wide or PaintMode.Right ? '<' : '\0';

            _cursor[1, 0] = new Pixel(c, _data[pos].BgColor.Contrast(), SCEColor.Transparent);
        }
    }

    private void UpdateCanvasPosition()
    {
        _data.Offset = -(Vec2I)_cameraPos.Round();

        UpdateCursor();
    }
}