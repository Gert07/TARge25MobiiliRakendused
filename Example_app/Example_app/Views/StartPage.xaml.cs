using Example_app.Services;
using Example_app.Views.Game;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;

namespace Example_app.Views;

public partial class StartPage : ContentPage
{
    private readonly SettingsService _settings;
    private readonly List<(Button Button, string Key)> _menuButtons = [];
    private readonly LanguageService _text = LanguageService.Instance;

    public StartPage(SettingsService settings, IServiceProvider services)
    {
        _settings = settings;
        var layout = new VerticalStackLayout { Padding = 20, Spacing = 15 };
        (string Key, Func<ContentPage> Create)[] pages =
        [
            ("GameTitle", () => services.GetRequiredService<TripsuPage>()),
            ("Settings", () => services.GetRequiredService<SettingsPage>()),
            ("MenuText", () => new TextPage()),
            ("MenuFigure", () => new FigurePage()),
            ("MenuTraffic", () => new ValgusfoorPage()),
            ("MenuTime", () => new DateTimePage()),
            ("MenuStepper", () => new StepperSliderPage()),
            ("MenuTree", () => new TreePage()),
            ("MenuPopup", () => new Popup()),
            ("MenuGrid", () => new GridPage()),
            ("MenuPhones", () => new ListViewPage()),
            ("MenuCarousel", () => new CarouselPage())
        ];
        foreach (var (key, create) in pages)
        {
            var button = new Button { Text = _text[key], FontSize = 22, CornerRadius = 10 };
            button.Clicked += async (_, _) => await Navigation.PushAsync(create());
            _menuButtons.Add((button, key));
            layout.Add(button);
        }

        var resetWelcome = new Button { BackgroundColor = Colors.DarkRed, TextColor = Colors.White };
        _menuButtons.Add((resetWelcome, "ResetWelcome"));
        resetWelcome.Clicked += async (_, _) =>
        {
            try
            {
                _settings.SetWelcomeSeen(false);
                await DisplayAlertAsync(_text["Welcome"], _text["WelcomeReset"], _text["OK"]);
            }
            catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
            {
                await DisplayAlertAsync(_text["Error"], _text["SaveFailed"], _text["OK"]);
            }
        };
        layout.Add(resetWelcome);
        Content = new ScrollView { Content = layout };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Title = _text["Home"];
        foreach (var (button, key) in _menuButtons) button.Text = _text[key];
        if (_settings.Current.HasSeenWelcome) return;
        try
        {
            _settings.SetWelcomeSeen(true);
            if (await DisplayAlertAsync(_text["Welcome"], _text["WelcomeQuestion"], _text["Yes"], _text["No"]))
                await DisplayAlertAsync(_text["GuideTitle"], _text["GuideText"], _text["OK"]);
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            await DisplayAlertAsync(_text["Error"], _text["SaveFailed"], _text["OK"]);
        }
    }
}

