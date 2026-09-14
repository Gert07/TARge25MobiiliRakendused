using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace Example_app;

public partial class StepperSliderPage : ContentPage
{
    readonly Border rectangle;
    readonly Label sizeLabel;
    readonly Label cornerLabel;
    readonly Label opacityLabel;

    public StepperSliderPage()
    {
        rectangle = new Border
        {
            WidthRequest = 160,
            HeightRequest = 110,
            BackgroundColor = Colors.CornflowerBlue,
            Stroke = Colors.MidnightBlue,
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
            HorizontalOptions = LayoutOptions.Center
        };

        var sizeStepper = CreateStepper(80, 280, 10, 160);
        var cornerStepper = CreateStepper(0, 55, 5, 12);
        var opacityStepper = CreateStepper(10, 100, 10, 100);

        sizeLabel = new Label { HorizontalOptions = LayoutOptions.Center };
        cornerLabel = new Label { HorizontalOptions = LayoutOptions.Center };
        opacityLabel = new Label { HorizontalOptions = LayoutOptions.Center };

        sizeStepper.ValueChanged += OnSizeChanged;
        cornerStepper.ValueChanged += OnCornerRadiusChanged;
        opacityStepper.ValueChanged += OnOpacityChanged;

        UpdateLabels(sizeStepper.Value, cornerStepper.Value, opacityStepper.Value);

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = "Ristküliku muutmine Stepperiga",
                    FontSize = 22,
                    HorizontalOptions = LayoutOptions.Center
                },
                rectangle,
                new BoxView { HeightRequest = 12, Opacity = 0 },
                new Label { Text = "Suurus", FontAttributes = FontAttributes.Bold },
                sizeLabel,
                sizeStepper,
                new Label { Text = "Nurkade ümarus", FontAttributes = FontAttributes.Bold },
                cornerLabel,
                cornerStepper,
                new Label { Text = "Läbipaistvus", FontAttributes = FontAttributes.Bold },
                opacityLabel,
                opacityStepper
            }
        };
    }

    static Stepper CreateStepper(double minimum, double maximum, double increment, double value) => new()
    {
        Minimum = minimum,
        Maximum = maximum,
        Increment = increment,
        Value = value,
        HorizontalOptions = LayoutOptions.Center
    };

    void OnSizeChanged(object? sender, ValueChangedEventArgs e)
    {
        rectangle.WidthRequest = e.NewValue;
        rectangle.HeightRequest = e.NewValue * 0.7;
        UpdateLabels(e.NewValue, null, null);
    }

    void OnCornerRadiusChanged(object? sender, ValueChangedEventArgs e)
    {
        ((RoundRectangle)rectangle.StrokeShape).CornerRadius = new CornerRadius(e.NewValue);
        UpdateLabels(null, e.NewValue, null);
    }

    void OnOpacityChanged(object? sender, ValueChangedEventArgs e)
    {
        rectangle.Opacity = e.NewValue / 100;
        UpdateLabels(null, null, e.NewValue);
    }

    void UpdateLabels(double? size, double? cornerRadius, double? opacity)
    {
        if (size.HasValue)
            sizeLabel.Text = $"Laius: {size.Value:F0}, kõrgus: {size.Value * 0.7:F0}";

        if (cornerRadius.HasValue)
            cornerLabel.Text = $"Raadius: {cornerRadius.Value:F0}";

        if (opacity.HasValue)
            opacityLabel.Text = $"Nähtavus: {opacity.Value:F0}%";
    }
}