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
    public Color Liquid { get; set; } = Colors.Goldenrod;
    public int Variant { get; set; }
    public double Phase { get; set; }

    public void Draw(ICanvas canvas, RectF r)
    {
        if (r.Width < 20 || r.Height < 20) return;

        var unit = Math.Min(r.Width, r.Height * 0.85f);
        var (bw, bh) = Variant switch
        {
            1 => (unit * 0.64f, unit * 0.64f),
            2 => (unit * 0.42f, unit * 0.82f),
            _ => (unit * 0.58f, unit * 0.62f)
        };
        var neckH = unit * 0.08f;
        var capH = unit * 0.16f;
        var total = bh + neckH + capH;

        var x = r.X + (r.Width - bw) / 2f;
        var capY = r.Y + (r.Height - total) / 2f;
        var neckY = capY + capH;
        var bodyY = neckY + neckH;
        var bottom = bodyY + bh;

        canvas.FillColor = Colors.Black.WithAlpha(0.28f);
        canvas.FillEllipse(x - bw * 0.05f, bottom - unit * 0.015f, bw * 1.1f, unit * 0.07f);

        canvas.FillColor = Colors.White.WithAlpha(0.18f);
        canvas.FillRectangle(x + bw * 0.37f, neckY - 1, bw * 0.26f, neckH + 2);

        var body = BodyPath(x, bodyY, bw, bh);
        canvas.FillColor = Colors.White.WithAlpha(0.08f);
        canvas.FillPath(body);

        canvas.SaveState();
        canvas.ClipPath(body);
        var level = bodyY + bh * 0.38f;
        var amp = bh * 0.018f;
        var wave = new PathF();
        wave.MoveTo(x, level);
        for (float px = 0; px <= bw; px += 4)
            wave.LineTo(x + px, level + MathF.Sin((float)Phase + px / bw * MathF.PI * 3f) * amp);
        wave.LineTo(x + bw, bottom);
        wave.LineTo(x, bottom);
        wave.Close();

        canvas.SetFillPaint(new LinearGradientPaint
        {
            StartColor = Liquid.WithAlpha(0.95f),
            EndColor = Color.FromHsla(Liquid.GetHue(), Liquid.GetSaturation(), Liquid.GetLuminosity() * 0.55f),
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        }, new RectF(x, level, bw, bottom - level));
        canvas.FillPath(wave);
        canvas.RestoreState();

        canvas.StrokeColor = Colors.White.WithAlpha(0.55f);
        canvas.StrokeSize = 1.5f;
        canvas.DrawPath(body);
        canvas.FillColor = Colors.White.WithAlpha(0.28f);
        canvas.FillRoundedRectangle(x + bw * 0.14f, bodyY + bh * 0.12f, bw * 0.08f, bh * 0.5f, bw * 0.04f);

        var capW = bw * 0.42f;
        var capX = x + (bw - capW) / 2f;
        canvas.SetFillPaint(new LinearGradientPaint
        {
            StartColor = Color.FromArgb("#F5E3B0"),
            EndColor = Color.FromArgb("#B08D3C"),
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        }, new RectF(capX, capY, capW, capH));
        canvas.FillRoundedRectangle(capX, capY, capW, capH, capW * 0.18f);
    }

    private PathF BodyPath(float x, float y, float w, float h)
    {
        var p = new PathF();
        if (Variant == 1) p.AppendEllipse(x, y, w, h);
        else p.AppendRoundedRectangle(x, y, w, h, Variant == 2 ? w * 0.28f : w * 0.14f);
        return p;
    }
}

public sealed class BottleView : GraphicsView
{
    public static readonly BindableProperty PerfumeProperty = BindableProperty.Create(
        nameof(Perfume), typeof(PerfumeSummary), typeof(BottleView), null,
        propertyChanged: (b, _, n) => ((BottleView)b).Update(n as PerfumeSummary));

    public static readonly BindableProperty AnimateLiquidProperty = BindableProperty.Create(
        nameof(AnimateLiquid), typeof(bool), typeof(BottleView), false);

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

    private void Update(PerfumeSummary? p)
    {
        if (p is null) return;
        _drawable.Liquid = BottleStyle.LiquidFor(p);
        _drawable.Variant = BottleStyle.VariantFor(p);
        Invalidate();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        this.AbortAnimation("wave");
        if (Handler is null || !AnimateLiquid) return;

        this.Animate("wave", v => { _drawable.Phase = v; Invalidate(); },
            0, Math.PI * 2, 16, 5000, Easing.Linear, null, () => true);
    }
}
