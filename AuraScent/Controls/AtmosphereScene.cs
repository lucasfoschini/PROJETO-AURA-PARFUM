using AuraScent.Atmospheres;

namespace AuraScent.Controls;

internal sealed class AtmosphereScene : IDrawable
{
    private static readonly RectF SceneBounds = new(0, 0, 1000, 400);
    private static readonly PathF FarRidge = Ridge(245, 209, 230, 163, 151, 225, 194, 239, 144, 140, 210, 177, 168, 229, 173, 220, 195);
    private static readonly PathF NearRidge = Ridge(279, 262, 305, 275, 301, 230, 290, 214, 262, 237, 272, 246, 300, 257, 286, 243, 269);
    private static readonly PathF Foreground = Ridge(349, 313, 353, 282, 335, 283, 327, 319, 352, 289, 330, 310, 354, 338, 286, 350, 321);
    private static readonly PathF FarHills = Hills(172);
    private static readonly PathF NearHills = Hills(222);
    private static readonly PathF Sunrays = CreateSunrays();
    private static readonly PathF Trees = CreateTrees();
    private static readonly LinearGradientPaint Scrim = new()
    {
        StartPoint = new Point(0, 0),
        EndPoint = new Point(0, 1),
        GradientStops = new[]
        {
            new PaintGradientStop(0, Colors.Black.WithAlpha(0.16f)),
            new PaintGradientStop(0.55f, Colors.Black.WithAlpha(0.38f)),
            new PaintGradientStop(1, Colors.Black.WithAlpha(0.8f))
        }
    };

    private readonly LinearGradientPaint _sky;
    private readonly RadialGradientPaint _halo;
    private readonly RadialGradientPaint _mist;
    private readonly (float X, float Y, float Size)[] _particles;
    private readonly Color _ridge;
    private readonly Color _particle;
    private readonly Color _treeColor;
    private readonly Color _rayColor;

    public Atmosphere Atmosphere { get; }
    public float Seconds { get; set; }

    public AtmosphereScene(Atmosphere atmosphere)
    {
        Atmosphere = atmosphere;
        _sky = new LinearGradientPaint
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops = new[]
            {
                new PaintGradientStop(0, atmosphere.Top),
                new PaintGradientStop(0.62f, atmosphere.Mid),
                new PaintGradientStop(1, atmosphere.Bottom)
            }
        };
        _halo = new RadialGradientPaint
        {
            Center = new Point(0.5, 0.5),
            Radius = 0.5,
            GradientStops = new[]
            {
                new PaintGradientStop(0, atmosphere.Accent.WithAlpha(0.63f)),
                new PaintGradientStop(0.32f, atmosphere.Glow.WithAlpha(0.4f)),
                new PaintGradientStop(1, atmosphere.Glow.WithAlpha(0))
            }
        };
        _ridge = atmosphere.Top.WithAlpha(0.58f);
        _treeColor = atmosphere.Bottom.WithAlpha(0.64f);
        _particle = atmosphere.Accent.WithAlpha(0.42f);
        _rayColor = atmosphere.Accent.WithAlpha(0.075f);
        _mist = new RadialGradientPaint
        {
            GradientStops = new[]
            {
                new PaintGradientStop(0, atmosphere.Accent.WithAlpha(0.19f)),
                new PaintGradientStop(1, atmosphere.Accent.WithAlpha(0))
            }
        };
        var random = new Random(73);
        var count = atmosphere is TemperateAtmosphere or SunnyAtmosphere ? 9 : 26;
        _particles = Enumerable.Range(0, count).Select(_ => (
            random.NextSingle() * 1000, random.NextSingle() * 400, 1 + random.NextSingle() * 2)).ToArray();
    }

    public void Draw(ICanvas canvas, RectF rect)
    {
        if (rect.Width <= 0 || rect.Height <= 0) return;
        canvas.SaveState();
        canvas.Translate(rect.X, rect.Y);
        canvas.Scale(rect.Width / SceneBounds.Width, rect.Height / SceneBounds.Height);
        canvas.SetFillPaint(_sky, SceneBounds);
        canvas.FillRectangle(SceneBounds);

        var drift = MathF.Sin(Seconds * MathF.Tau / 10);
        var light = new RectF(420 + 26 * drift, -172, 643, 540);
        canvas.SetFillPaint(_halo, light);
        canvas.FillRectangle(light);

        if (Atmosphere is NightAtmosphere)
        {
            canvas.FillColor = Atmosphere.Accent;
            canvas.FillCircle(781, 92, 23);
            canvas.FillColor = Atmosphere.Mid;
            canvas.FillCircle(792, 83, 21);
        }
        else if (Atmosphere is SunnyAtmosphere)
        {
            canvas.FillColor = _rayColor;
            canvas.FillPath(Sunrays);
        }

        var hasHills = Atmosphere is TemperateAtmosphere;
        canvas.FillColor = _ridge;
        canvas.FillPath(hasHills ? FarHills : FarRidge);
        canvas.FillColor = Atmosphere.Top;
        canvas.Alpha = 0.7f;
        canvas.FillPath(hasHills ? NearHills : NearRidge);
        canvas.Alpha = 1;

        if (Atmosphere is RainyAtmosphere or ColdAtmosphere)
        {
            var haze = new RectF(-140 + 65 * drift, 152, 1240, 208);
            canvas.SetFillPaint(_mist, haze);
            canvas.FillRectangle(haze);
        }

        canvas.FillColor = _treeColor;
        canvas.FillPath(Trees);
        canvas.FillPath(Foreground);

        canvas.FillColor = _particle;
        canvas.StrokeColor = _particle;
        canvas.StrokeSize = 1.3f;
        foreach (var (x, y, size) in _particles)
        {
            switch (Atmosphere)
            {
                case RainyAtmosphere:
                    var rainY = (y + Seconds * (48 + size * 8)) % 420 - 16;
                    canvas.DrawLine(x, rainY, x - size * 5, rainY + size * 7);
                    break;
                case NightAtmosphere when y < 180:
                    canvas.Alpha = 0.65f + 0.3f * MathF.Sin(Seconds + x);
                    canvas.FillCircle(x, y, size * 0.65f);
                    break;
                case ColdAtmosphere or TemperateAtmosphere or SunnyAtmosphere:
                    canvas.FillCircle(x + drift * size * 7, y, size * 0.7f);
                    break;
            }
        }

        canvas.Alpha = 1;
        canvas.SetFillPaint(Scrim, SceneBounds);
        canvas.FillRectangle(SceneBounds);
        canvas.RestoreState();
    }

    private static PathF Ridge(params float[] heights)
    {
        var path = new PathF();
        path.MoveTo(0, heights[0]);
        for (var i = 1; i < heights.Length; i++)
            path.LineTo(1000f * i / (heights.Length - 1), heights[i]);
        path.LineTo(1000, 400);
        path.LineTo(0, 400);
        path.Close();
        return path;
    }

    private static PathF CreateTrees()
    {
        var path = new PathF();
        var random = new Random(98);
        for (var i = 0; i < 22; i++)
        {
            var x = random.Next(0, 1000);
            var y = random.Next(282, 389);
            var height = random.Next(28, 74);
            path.MoveTo(x, y - height);
            path.LineTo(x + height * 0.29f, y - 6);
            path.LineTo(x + 3, y - 6);
            path.LineTo(x + 3, y + 4);
            path.LineTo(x - 3, y + 4);
            path.LineTo(x - 3, y - 6);
            path.LineTo(x - height * 0.29f, y - 6);
            path.Close();
        }
        return path;
    }

    private static PathF Hills(float y)
    {
        var path = new PathF();
        path.MoveTo(0, y + 36);
        path.CurveTo(135, y - 84, 238, y - 14, 345, y + 32);
        path.CurveTo(464, y + 100, 533, y + 19, 620, y - 12);
        path.CurveTo(760, y - 90, 867, y - 50, 1000, y + 43);
        path.LineTo(1000, 400);
        path.LineTo(0, 400);
        path.Close();
        return path;
    }

    private static PathF CreateSunrays()
    {
        var path = new PathF();
        path.MoveTo(768, 42);
        path.LineTo(88, 198);
        path.LineTo(180, 257);
        path.Close();
        path.MoveTo(768, 42);
        path.LineTo(366, 284);
        path.LineTo(531, 303);
        path.Close();
        path.MoveTo(768, 42);
        path.LineTo(914, 280);
        path.LineTo(1000, 241);
        path.Close();
        return path;
    }
}
