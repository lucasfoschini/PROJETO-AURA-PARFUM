using AuraScent.Models;

namespace AuraScent.Controls;

public static class BottleStyle
{
    private static readonly (string Word, string Hex)[] Palette =
    {
        ("citrus", "#F6D04D"), ("aquatic", "#4FC3D9"), ("fresh", "#7ED6B5"), ("green", "#6FBF73"),
        ("rose", "#E85D8A"), ("floral", "#F28BB0"), ("oud", "#8B4A2B"), ("amber", "#E39A3B"),
        ("spic", "#C8602A"), ("tobacco", "#9A5B2E"), ("leather", "#6B4533"), ("wood", "#A9743F"),
        ("gourmand", "#D9A066"), ("vanilla", "#EBC98A"), ("musk", "#D8C3D9")
    };

    public static Color LiquidFor(PerfumeSummary p)
    {
        var key = $"{p.Family} {p.Name}".ToLowerInvariant();
        foreach (var (word, hex) in Palette)
            if (key.Contains(word)) return Color.FromArgb(hex);
        return Color.FromHsla((StableHash(key) % 360) / 360.0, 0.55, 0.55);
    }

    public static int VariantFor(PerfumeSummary p) => StableHash(p.Name) % 3;

    private static int StableHash(string s)
    {
        uint h = 17;
        unchecked { foreach (var c in s) h = h * 31 + c; }
        return (int)(h & 0x7FFFFFFF);
    }
}

public sealed class BottleDrawable : IDrawable
{
    private static readonly Color Ink = Color.FromArgb("#101017");
    private static readonly Color Silver = Colors.White.WithAlpha(0.58f);
    private static readonly LinearGradientPaint Shelf = new()
    {
        StartColor = Color.FromArgb("#19171F"),
        EndColor = Color.FromArgb("#08090D"),
        StartPoint = new Point(0, 0),
        EndPoint = new Point(0.7, 1)
    };
    private static readonly LinearGradientPaint Cap = new()
    {
        GradientStops = new[] { new PaintGradientStop(0, Color.FromArgb("#383640")), new(0.35f, Ink), new(1, Colors.Black) },
        StartPoint = new Point(0, 0),
        EndPoint = new Point(1, 0)
    };
    private static readonly LinearGradientPaint Reflection = new()
    {
        StartPoint = new Point(0, 0),
        EndPoint = new Point(1, 0.18),
        GradientStops = new[]
        {
            new PaintGradientStop(0, Colors.Transparent),
            new(0.5f, Colors.White.WithAlpha(0.48f)),
            new(1, Colors.Transparent)
        }
    };

    private PerfumeSummary? _perfume;
    private PathF _body = new();
    private Color _liquid = Colors.Transparent;
    private LinearGradientPaint _glass = new();
    private RadialGradientPaint _glow = new();
    private float _width;
    private float _bottom;
    private float _left;

    public float Phase { get; set; }

    public void SetPerfume(PerfumeSummary? perfume)
    {
        _perfume = perfume;
        if (perfume is null) return;
        _liquid = BottleStyle.LiquidFor(perfume);
        var variant = BottleStyle.VariantFor(perfume);
        _width = variant == 2 ? 62 : 86;
        _bottom = variant == 2 ? 122 : 118;
        _left = (120 - _width) / 2;
        _body = new PathF();
        if (variant == 1)
        {
            _body.AppendRoundedRectangle(_left, 36, _width, _bottom - 36, 19);
        }
        else
        {
            _body.MoveTo(46, 31);
            _body.LineTo(74, 31);
            _body.LineTo(120 - _left - 6, 43);
            _body.QuadTo(120 - _left, 47, 120 - _left, 54);
            _body.LineTo(120 - _left, _bottom - 9);
            _body.QuadTo(120 - _left, _bottom, 120 - _left - 8, _bottom);
            _body.LineTo(_left + 8, _bottom);
            _body.QuadTo(_left, _bottom, _left, _bottom - 9);
            _body.LineTo(_left, 54);
            _body.QuadTo(_left, 47, _left + 6, 43);
            _body.Close();
        }

        _glass = new LinearGradientPaint
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 0),
            GradientStops = new[]
            {
                new PaintGradientStop(0, Silver),
                new(0.15f, _liquid.WithAlpha(0.9f)),
                new(0.52f, Color.FromHsla(_liquid.GetHue(), _liquid.GetSaturation(), _liquid.GetLuminosity() * 0.57f)),
                new(0.83f, _liquid.WithAlpha(0.92f)),
                new(1, Silver)
            }
        };
        _glow = new RadialGradientPaint
        {
            GradientStops = new[]
            {
                new PaintGradientStop(0, _liquid.WithAlpha(0.5f)),
                new(0.46f, _liquid.WithAlpha(0.15f)),
                new(1, _liquid.WithAlpha(0))
            }
        };
    }

    public void Draw(ICanvas canvas, RectF rect)
    {
        canvas.SetFillPaint(Shelf, rect);
        canvas.FillRectangle(rect);
        if (_perfume is null || rect.Width <= 0 || rect.Height <= 0) return;

        var glow = new RectF(rect.X + rect.Width * 0.05f, rect.Y, rect.Width * 0.9f, rect.Height);
        canvas.SetFillPaint(_glow, glow);
        canvas.FillRectangle(glow);

        var scale = Math.Min(rect.Width / 131, rect.Height / 139);
        canvas.SaveState();
        canvas.Translate(rect.X + (rect.Width - 120 * scale) / 2, rect.Y + (rect.Height - 132 * scale) / 2);
        canvas.Scale(scale, scale);

        canvas.FillColor = Colors.Black.WithAlpha(0.48f);
        canvas.FillEllipse(_left - 5, _bottom - 2, _width + 10, 11);
        canvas.FillColor = _liquid;
        canvas.FillRectangle(50, 23, 20, 13);

        canvas.SetFillPaint(_glass, new RectF(_left, 31, _width, _bottom - 31));
        canvas.FillPath(_body);
        DrawReflection(canvas);
        canvas.StrokeColor = Silver;
        canvas.StrokeSize = 0.85f;
        canvas.DrawPath(_body);
        canvas.DrawLine(_left + 5, 52, _left + 5, _bottom - 8);
        canvas.DrawLine(_left + 5, _bottom - 6, 115 - _left, _bottom - 6);

        var liquidLevel = 58 + MathF.Sin(Phase) * 2.5f;
        canvas.StrokeColor = Colors.White.WithAlpha(0.25f);
        canvas.DrawLine(_left + 6, liquidLevel, 114 - _left, liquidLevel);

        var cap = new RectF(46, 6, 28, 21);
        canvas.SetFillPaint(Cap, cap);
        canvas.FillRoundedRectangle(cap.X, cap.Y, cap.Width, cap.Height, 3);
        canvas.DrawRoundedRectangle(cap.X, cap.Y, cap.Width, cap.Height, 3);

        var labelWidth = _width * 0.69f;
        var labelX = (120 - labelWidth) / 2;
        canvas.FillColor = Ink;
        canvas.FillRoundedRectangle(labelX, 68, labelWidth, 33, 1.5f);
        canvas.StrokeColor = _liquid.WithAlpha(0.54f);
        canvas.DrawRoundedRectangle(labelX + 2, 70, labelWidth - 4, 29, 1);
        canvas.FontColor = Colors.White;
        canvas.FontSize = 5.5f;
        canvas.DrawString("A U R A", 60, 81, HorizontalAlignment.Center);
        canvas.FontSize = 8;
        canvas.DrawString(_perfume.Initial, 60, 94, HorizontalAlignment.Center);
        canvas.RestoreState();
    }

    private void DrawReflection(ICanvas canvas)
    {
        const float beamWidth = 28;
        var progress = (Phase / MathF.Tau - 0.1f) / 0.65f;
        if (progress is <= 0 or >= 1) return;

        var beam = new RectF(_left - beamWidth + (_width + beamWidth) * progress, 31, beamWidth, _bottom - 31);
        canvas.SaveState();
        canvas.ClipPath(_body);
        canvas.Alpha = MathF.Sin(progress * MathF.PI);
        canvas.SetFillPaint(Reflection, beam);
        canvas.FillRectangle(beam);
        canvas.RestoreState();
    }
}

public sealed class BottleView : GraphicsView
{
    public static readonly BindableProperty PerfumeProperty = BindableProperty.Create(
        nameof(Perfume), typeof(PerfumeSummary), typeof(BottleView), null,
        propertyChanged: (view, _, value) => ((BottleView)view).Update((PerfumeSummary?)value));

    public static readonly BindableProperty AnimateLiquidProperty = BindableProperty.Create(
        nameof(AnimateLiquid), typeof(bool), typeof(BottleView), false,
        propertyChanged: (view, _, _) => ((BottleView)view).UpdateMotion());

    private readonly BottleDrawable _drawable = new();

    public PerfumeSummary? Perfume
    {
        get => (PerfumeSummary?)GetValue(PerfumeProperty);
        set => SetValue(PerfumeProperty, value);
    }

    public bool AnimateLiquid
    {
        get => (bool)GetValue(AnimateLiquidProperty);
        set => SetValue(AnimateLiquidProperty, value);
    }

    public BottleView()
    {
        Drawable = _drawable;
        InputTransparent = true;
    }

    private void Update(PerfumeSummary? perfume)
    {
        _drawable.SetPerfume(perfume);
        Invalidate();
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        if (args.NewHandler is null) this.AbortAnimation("liquid");
        base.OnHandlerChanging(args);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        UpdateMotion();
    }

    private void UpdateMotion()
    {
        this.AbortAnimation("liquid");
        if (Handler is null || !AnimateLiquid || !Motion.IsEnabled)
        {
            _drawable.Phase = 0;
            Invalidate();
            return;
        }

        this.Animate("liquid", value =>
        {
            _drawable.Phase = (float)value;
            Invalidate();
        }, 0, Math.Tau, 33, 8500, Easing.Linear, repeat: () => AnimateLiquid && Handler is not null);
    }
}
