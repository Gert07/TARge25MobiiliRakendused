using AlienBreach.Services;
namespace AlienBreach.Rendering;

// A static holographic scene makes the translucent panels readable without animation overhead.
public sealed class HudBackdrop : IDrawable
{
    public void Draw(ICanvas canvas, RectF bounds)
    {
        canvas.FillColor = Color.FromArgb("#050E18");
        canvas.FillRectangle(bounds);
        var orbX = bounds.Width * .35f;
        var orbY = bounds.Height * .52f;
        var radius = Math.Min(bounds.Width * .36f, bounds.Height * .55f);
        canvas.FillColor = Color.FromArgb("#091E2B");
        canvas.FillCircle(orbX, orbY, radius);
        canvas.FillColor = Color.FromArgb("#0B2430");
        canvas.FillCircle(bounds.Width * .95f, bounds.Height * .05f, radius * 1.6f);
        canvas.StrokeColor = Color.FromArgb("#162B3C");
        canvas.StrokeSize = 1;
        for (var x = 0f; x < bounds.Width; x += 48) canvas.DrawLine(x, 0, x, bounds.Height);
        for (var y = 0f; y < bounds.Height; y += 48) canvas.DrawLine(0, y, bounds.Width, y);
        canvas.StrokeColor = Color.FromArgb("#29495A");
        canvas.StrokeSize = 1.5f;
        canvas.DrawCircle(orbX, orbY, radius);
        canvas.DrawCircle(orbX, orbY, radius * .84f);
        canvas.DrawEllipse(orbX - radius, orbY - radius * .25f, radius * 2, radius * .5f);
        canvas.DrawEllipse(orbX - radius * .25f, orbY - radius, radius * .5f, radius * 2);
        canvas.StrokeColor = Color.FromArgb("#397B7D");
        canvas.DrawLine(orbX - radius - 15, orbY, orbX - radius + 15, orbY);
        canvas.DrawLine(orbX + radius - 15, orbY, orbX + radius + 15, orbY);
        canvas.DrawLine(orbX, orbY - radius - 15, orbX, orbY - radius + 15);
        canvas.DrawLine(orbX, orbY + radius - 15, orbX, orbY + radius + 15);
        for (var i = 0; i < 65; i++)
        {
            var x = ((i * 197 + 47) % 997) / 997f * bounds.Width;
            var y = ((i * 331 + 83) % 991) / 991f * bounds.Height;
            canvas.FillColor = i % 4 == 0 ? Color.FromArgb("#82CBBE") : Color.FromArgb("#3E6779");
            canvas.FillCircle(x, y, i % 4 == 0 ? 1.4f : .8f);
        }
        canvas.StrokeColor = Color.FromArgb("#184052");
        for (var y = 2f; y < bounds.Height; y += 5)
        {
            canvas.Alpha = .18f;
            canvas.DrawLine(0, y, bounds.Width, y);
        }
        canvas.Alpha = 1;
    }
}
