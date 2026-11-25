using AuraScent.Atmospheres;

namespace AuraScent.Controls;

public sealed class AtmosphereIcon : GraphicsView, IDrawable
{
    public static readonly BindableProperty AtmosphereProperty = BindableProperty.Create(
        nameof(Atmosphere), typeof(Atmosphere), typeof(AtmosphereIcon), null,
        propertyChanged: (view, _, _) => ((AtmosphereIcon)view).Invalidate());

    public Atmosphere? Atmosphere
    {
        get => (Atmosphere?)GetValue(AtmosphereProperty);
        set => SetValue(AtmosphereProperty, value);
    }

    public AtmosphereIcon()
    {
        Drawable = this;
        InputTransparent = true;
    }

    void IDrawable.Draw(ICanvas canvas, RectF rect)
    {
        if (Atmosphere is not { } atmosphere) return;
        var size = Math.Min(rect.Width, rect.Height);
        canvas.SaveState();
        canvas.Translate(rect.Center.X - size / 2, rect.Center.Y - size / 2);
        canvas.Scale(size / 24, size / 24);
        canvas.StrokeColor = atmosphere.Accent;
        canvas.StrokeSize = 1.4f;
        canvas.StrokeLineCap = LineCap.Round;

        switch (atmosphere)
        {
            case NightAtmosphere:
                var moon = new PathF();
                moon.MoveTo(10, 3);
                moon.CurveTo(4, 4, 2, 10, 4, 15);
                moon.CurveTo(7, 22, 18, 21, 20, 14);
                moon.CurveTo(13, 16, 7, 10, 10, 3);
                canvas.DrawPath(moon);
                canvas.DrawLine(17, 4, 17, 9);
                canvas.DrawLine(14.5f, 6.5f, 19.5f, 6.5f);
                break;
            case RainyAtmosphere:
                var cloud = new PathF();
                cloud.MoveTo(6, 14);
                cloud.CurveTo(0, 14, 2, 6, 7, 7);
                cloud.CurveTo(8, 1, 16, 2, 17, 7);
                cloud.CurveTo(23, 6, 23, 14, 18, 14);
                cloud.LineTo(6, 14);
                canvas.DrawPath(cloud);
                for (var x = 7; x <= 19; x += 5) canvas.DrawLine(x, 17, x - 2, 21);
                break;
            case ColdAtmosphere:
                for (var i = 0; i < 6; i++)
                {
                    canvas.DrawLine(12, 12, 12, 3);
                    canvas.DrawLine(12, 7, 9.5f, 5);
                    canvas.DrawLine(12, 7, 14.5f, 5);
                    canvas.Rotate(60, 12, 12);
                }
                break;
            case SunnyAtmosphere:
                canvas.DrawCircle(12, 12, 5);
                for (var i = 0; i < 8; i++)
                {
                    canvas.DrawLine(12, 2, 12, 4);
                    canvas.Rotate(45, 12, 12);
                }
                break;
            default:
                var leaf = new PathF();
                leaf.MoveTo(6, 18);
                leaf.CurveTo(0, 10, 9, 3, 21, 3);
                leaf.CurveTo(21, 14, 17, 23, 6, 18);
                canvas.DrawPath(leaf);
                canvas.DrawLine(3, 22, 15, 10);
                break;
        }

        canvas.RestoreState();
    }
}
