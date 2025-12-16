using Microsoft.Maui.Graphics;

namespace StoreProgram.Components;

public record ChartPoint(string Label, double Value);

public sealed class SalesBarChartDrawable : IDrawable
{
    private readonly IReadOnlyList<ChartPoint> _points;
    private readonly Color _barColor;
    private readonly Color _axisColor;
    private readonly Color _textColor;

    public SalesBarChartDrawable(
        IReadOnlyList<ChartPoint> points,
        Color? barColor = null,
        Color? axisColor = null,
        Color? textColor = null)
    {
        _points = points;
        _barColor = barColor ?? Color.FromArgb("#7C3AED"); // Primary
        _axisColor = axisColor ?? Color.FromArgb("#CBD5E1"); // Gray300
        _textColor = textColor ?? Color.FromArgb("#0F172A"); // Gray900
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();

        // Empty state
        if (_points.Count == 0)
        {
            canvas.FontSize = 14;
            canvas.FontColor = _textColor.WithAlpha(0.7f);
            canvas.DrawString(
                "Belum ada data penjualan untuk ditampilkan.",
                dirtyRect,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
            canvas.RestoreState();
            return;
        }

        // Layout
        const float leftPad = 8;
        const float rightPad = 8;
        const float topPad = 8;
        const float bottomPad = 26; // label area

        var plot = new RectF(
            dirtyRect.Left + leftPad,
            dirtyRect.Top + topPad,
            dirtyRect.Width - leftPad - rightPad,
            dirtyRect.Height - topPad - bottomPad);

        var max = (float)Math.Max(1, _points.Max(p => p.Value));

        // Axis line
        canvas.StrokeColor = _axisColor;
        canvas.StrokeSize = 1;
        canvas.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom);

        // Bars
        int n = _points.Count;
        float gap = n <= 12 ? 6 : 3;
        float barWidth = (plot.Width - gap * (n - 1)) / n;
        barWidth = Math.Max(4, barWidth);

        canvas.FillColor = _barColor;

        for (int i = 0; i < n; i++)
        {
            var p = _points[i];
            float h = (float)(p.Value / max) * plot.Height;
            float x = plot.Left + i * (barWidth + gap);
            float y = plot.Bottom - h;

            var barRect = new RectF(x, y, barWidth, h);
            canvas.FillRoundedRectangle(barRect, 6);
        }

        // Labels (every k to avoid crowded)
        int step = n switch
        {
            <= 8 => 1,
            <= 16 => 2,
            <= 31 => 3,
            _ => 5
        };

        canvas.FontSize = 10;
        canvas.FontColor = _textColor.WithAlpha(0.8f);

        for (int i = 0; i < n; i += step)
        {
            var p = _points[i];
            float x = plot.Left + i * (barWidth + gap) + barWidth / 2;
            float y = plot.Bottom + 6;
            canvas.DrawString(p.Label, x, y, HorizontalAlignment.Center);
        }

        canvas.RestoreState();
    }
}
