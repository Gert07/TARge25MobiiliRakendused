using AlienBreach.Services;
using Microsoft.Maui.Controls.Shapes;

namespace AlienBreach.Views;

internal static class Theme
{
    public static Color Mint => Color.FromArgb("#65EFBA");
    public static Color Muted => Color.FromArgb("#9DBAC8");
    public static Color Amber => Color.FromArgb("#FFCA76");
    public static Label Text(string text, double size = 14, Color? color = null) => new()
    {
        Text = text, FontSize = size, TextColor = color ?? Color.FromArgb("#EDF9FC"),
        LineBreakMode = LineBreakMode.WordWrap
    };

    public static LinearGradientBrush GlassBackground() => new()
    {
        StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
        GradientStops = new GradientStopCollection
        {
            new(Color.FromArgb("#68395568"), 0),
            new(Color.FromArgb("#78132535"), .45f),
            new(Color.FromArgb("#48364D62"), 1)
        }
    };

    public static Border Card(View content, Thickness? padding = null) => new()
    {
        Content = content, Padding = padding ?? new Thickness(18),
        Background = GlassBackground(),
        Stroke = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0), EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new(Color.FromArgb("#A0BDFBF0"), 0),
                new(Color.FromArgb("#405398AE"), .5f),
                new(Color.FromArgb("#285D9FAF"), 1)
            }
        },
        StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = 16 },
        Shadow = new Shadow { Brush = new SolidColorBrush(Mint), Opacity = .12f, Radius = 18, Offset = new Point(0, 5) }
    };

    public static Button Action(string text, bool primary = false) => new()
    {
        Text = text, BackgroundColor = primary ? Mint : Color.FromArgb("#50345065"),
        TextColor = primary ? Color.FromArgb("#07131A") : Color.FromArgb("#EDF9FC"),
        BorderColor = primary ? Color.FromArgb("#A5FFDF") : Color.FromArgb("#706EB5C4"),
        BorderWidth = 1, CornerRadius = 10, MinimumHeightRequest = 44,
        FontAttributes = FontAttributes.Bold, FontSize = 12, CharacterSpacing = 1
    };

    public static View Metric(Label value, string title) => new VerticalStackLayout
    {
        Spacing = 5, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
        Children = { value, new Label { Text = title, FontSize = 10, CharacterSpacing = 1.3,
            TextColor = Muted, HorizontalTextAlignment = TextAlignment.Center } }
    };
}
