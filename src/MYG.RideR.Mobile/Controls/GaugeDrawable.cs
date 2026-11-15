using Microsoft.Maui.Graphics;

namespace MYG.RideR.Mobile.Controls;

/// <summary>270° açık-alt hız göstergesi: gradyan yay, çentikler ve 0–240 sayıları.</summary>
public sealed class GaugeDrawable : IDrawable
{
    private const float StartAngle = 135f;
    private const float SweepAngle = 270f;

    private static readonly Color Blue = Color.FromArgb("#00BFFF");
    private static readonly Color Red = Color.FromArgb("#FF3B30");

    public double Speed { get; set; }

    public double MaxValue { get; set; } = 240;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var cx = dirtyRect.Center.X;
        var cy = dirtyRect.Center.Y;
        var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        var outer = size / 2f - 14f;

        canvas.FillColor = Color.FromArgb("#0B1118");
        canvas.FillCircle(cx, cy, size / 2f - 4f);
        canvas.StrokeColor = Color.FromArgb("#1E2732");
        canvas.StrokeSize = 2;
        canvas.DrawCircle(cx, cy, size / 2f - 4f);

        var max = MaxValue > 0 ? MaxValue : 240;
        var ratio = (float)Math.Clamp(Speed / max, 0, 1);
        const int segments = 135;
        canvas.StrokeSize = 10;
        canvas.StrokeLineCap = LineCap.Butt;
        for (var i = 0; i < segments; i++)
        {
            var t0 = i / (float)segments;
            var t1 = (i + 1) / (float)segments;
            var color = Lerp(Blue, Red, t0);
            canvas.StrokeColor = t0 <= ratio ? color : color.WithAlpha(0.18f);
            var a0 = Point(cx, cy, outer, StartAngle + SweepAngle * t0);
            var a1 = Point(cx, cy, outer, StartAngle + SweepAngle * t1 + 0.4f);
            canvas.DrawLine(a0, a1);
        }

        canvas.FontColor = Color.FromArgb("#9AA7B8");
        canvas.FontSize = 13;
        var majorStep = max <= 20 ? 2 : max <= 60 ? 10 : 20;
        var minorStep = majorStep / 2;
        for (var v = 0; v <= max; v += minorStep)
        {
            var angle = StartAngle + SweepAngle * (float)(v / max);
            var major = v % majorStep == 0;
            canvas.StrokeSize = major ? 2.5f : 1.2f;
            canvas.StrokeColor = major ? Colors.White : Color.FromArgb("#5C6878");
            var inner = outer - (major ? 18f : 12f);
            canvas.DrawLine(Point(cx, cy, outer - 10f, angle), Point(cx, cy, inner - 4f, angle));
            if (major)
            {
                var p = Point(cx, cy, outer - 40f, angle);
                canvas.DrawString(v.ToString(), p.X - 20, p.Y - 9, 40, 18,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }
    }

    private static PointF Point(float cx, float cy, float r, float degrees)
    {
        var rad = degrees * Math.PI / 180.0;
        return new PointF(cx + r * (float)Math.Cos(rad), cy + r * (float)Math.Sin(rad));
    }

    private static Color Lerp(Color a, Color b, float t) =>
        new(a.Red + (b.Red - a.Red) * t, a.Green + (b.Green - a.Green) * t, a.Blue + (b.Blue - a.Blue) * t);
}
