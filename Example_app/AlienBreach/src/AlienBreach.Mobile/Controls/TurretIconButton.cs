using AlienBreach.Services;
using AlienBreach.Core.Turrets;
using AlienBreach.Rendering;

namespace AlienBreach.Controls;

public sealed class TurretIconButton : ContentView
{
    private readonly Button button;
    private readonly Label price;
    private readonly GraphicsView icon;
    private readonly TurretDefinition definition;
    public event EventHandler? Clicked;

    public TurretIconButton(TurretDefinition definition)
    {
        this.definition = definition;
        WidthRequest = 54;
        HeightRequest = 60;
        button = new Button
        {
            Text = "", Padding = 0, CornerRadius = 9, BorderWidth = 1,
            BackgroundColor = Color.FromArgb("#C00B1E29"), BorderColor = Color.FromArgb("#5560919E"),
            MinimumWidthRequest = 44, MinimumHeightRequest = 44
        };
        SemanticProperties.SetDescription(button, L.F("TurretDescription", L.T("Turret_" + definition.Type), definition.Cost));
        ToolTipProperties.SetText(button, definition.Name);
        button.Clicked += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);
        var root = new Grid();
        root.Add(button);
        var art = new Grid
        {
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) },
            Padding = new Thickness(4, 3, 4, 5), InputTransparent = true
        };
        icon = new GraphicsView { Drawable = new IconDrawable(definition), InputTransparent = true };
        price = new Label { Text = $"${definition.Cost}", FontSize = 10, FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center, TextColor = Colors.White, InputTransparent = true };
        art.Add(icon, 0, 0);
        art.Add(price, 0, 1);
        root.Add(art);
        Content = root;
    }

    public void Refresh(bool active, bool selected, bool affordable)
    {
        button.IsEnabled = active;
        var color = GameRenderer.ShotColor(definition.ShotKind);
        button.BorderColor = selected ? color : Color.FromArgb("#5560919E");
        button.BorderWidth = selected ? 2 : 1;
        button.BackgroundColor = selected ? Color.FromArgb("#DD214653") : Color.FromArgb("#C00B1E29");
        price.TextColor = affordable ? color : Color.FromArgb("#82959F");
        icon.Opacity = affordable ? 1 : .45;
    }

    private sealed class IconDrawable(TurretDefinition definition) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF bounds)
        {
            var scale = Math.Min(bounds.Width / 62, bounds.Height / 46);
            canvas.SaveState();
            canvas.Translate(bounds.Width / 2 - 6 * scale, bounds.Height / 2);
            canvas.Scale(scale, scale);
            canvas.FillColor = Color.FromArgb("#436574");
            canvas.FillRoundedRectangle(-17, -17, 34, 34, 6);
            canvas.FillColor = GameRenderer.ShotColor(definition.ShotKind);
            switch (definition.Type)
            {
                case TurretType.Repeater:
                    canvas.FillRoundedRectangle(-3, -11, 34, 7, 2);
                    canvas.FillRoundedRectangle(-3, 4, 34, 7, 2);
                    break;
                case TurretType.Cannon:
                    canvas.FillRoundedRectangle(-5, -12, 36, 24, 5);
                    canvas.FillColor = Color.FromArgb("#263A4B");
                    canvas.FillCircle(24, 0, 8);
                    break;
                case TurretType.Cryo:
                    canvas.StrokeColor = GameRenderer.ShotColor(definition.ShotKind);
                    canvas.StrokeSize = 3;
                    canvas.DrawCircle(0, 0, 15);
                    for (var i = 0; i < 6; i++) { canvas.Rotate(60); canvas.DrawLine(0, 0, 22, 0); }
                    break;
                case TurretType.Railgun:
                    canvas.FillRoundedRectangle(-8, -8, 46, 5, 2);
                    canvas.FillRoundedRectangle(-8, 3, 46, 5, 2);
                    canvas.FillRectangle(-2, -12, 12, 24);
                    break;
                default:
                    canvas.FillRoundedRectangle(-3, -5, 34, 10, 2);
                    canvas.FillCircle(-5, 0, 10);
                    break;
            }
            canvas.RestoreState();
        }
    }
}
