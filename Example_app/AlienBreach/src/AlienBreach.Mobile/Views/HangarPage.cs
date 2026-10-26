using AlienBreach.Services;
using AlienBreach.Core.Persistence;

namespace AlienBreach.Views;

public sealed class HangarPage : HudPage
{
    private bool navigating;
    private readonly IGameRepository repository;
    private readonly SettingsService settings;
    private readonly GameAudioService audio;
    private string language = "";

    public HangarPage(IGameRepository repository, SettingsService settings, GameAudioService audio) : base(L.T("CommandDeck"), false)
    {
        this.repository = repository;
        this.settings = settings;
        this.audio = audio;
        BuildMenu();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        settings.Changed -= SettingsChanged;
        settings.Changed += SettingsChanged;
        if (language != settings.Current.Language) BuildMenu();
    }

    protected override void OnDisappearing()
    {
        settings.Changed -= SettingsChanged;
        base.OnDisappearing();
    }

    private void SettingsChanged(object? sender, EventArgs args)
    {
        if (language != settings.Current.Language) BuildMenu();
    }

    private void BuildMenu()
    {
        language = settings.Current.Language;
        SetHeading(L.T("CommandDeck"));
        var layout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(320) },
            ColumnSpacing = 28
        };
        var title = new Label
        {
            Text = "ALIEN\nBREACH", FontSize = 72, FontAttributes = FontAttributes.Bold,
            LineHeight = 1, CharacterSpacing = 3, TextColor = Color.FromArgb("#EDF9FC")
        };
        var identity = new VerticalStackLayout
        {
            Spacing = 12, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(24, 0),
            Children = { new Label { Text = L.T("OrbitalDefense"), FontSize = 11, CharacterSpacing = 3,
                TextColor = Theme.Mint }, title }
        };
        layout.Add(identity, 0);
        var play = Theme.Action(L.T("Play"), true);
        var records = Theme.Action(L.T("RecordsButton"));
        var history = Theme.Action(L.T("HistoryButton"));
        var settingsButton = Theme.Action(L.T("SettingsButton"));
        var buttons = new[] { play, records, history, settingsButton };
        foreach (var button in buttons) button.HeightRequest = 54;
        async Task OpenAsync(Func<Page> createPage)
        {
            if (navigating) return;
            navigating = true;
            foreach (var button in buttons) button.IsEnabled = false;
            try { await Navigation.PushAsync(createPage()); }
            finally
            {
                navigating = false;
                foreach (var button in buttons) button.IsEnabled = true;
            }
        }
        play.Clicked += async (_, _) => await OpenAsync(() => new MapSelectionPage(repository, settings, audio));
        records.Clicked += async (_, _) => await OpenAsync(() => new RecordsPage(repository));
        history.Clicked += async (_, _) => await OpenAsync(() => new HistoryPage(repository));
        settingsButton.Clicked += async (_, _) => await OpenAsync(() => new SettingsPage(settings));
        var menu = new VerticalStackLayout { Spacing = 12, Children =
        { new Label { Text = L.T("Command"), FontSize = 11, CharacterSpacing = 3, TextColor = Theme.Muted }, play, records, history, settingsButton } };
        var panel = Theme.Card(menu, 24);
        panel.VerticalOptions = LayoutOptions.Center;
        layout.Add(panel, 1);
        Body.Content = layout;
        if (resizeMenu is not null) SizeChanged -= resizeMenu;
        resizeMenu = (_, _) =>
        {
            var compact = Height < 480 || Width < 800;
            title.FontSize = compact ? 44 : 72;
            title.CharacterSpacing = compact ? 1 : 3;
            identity.Margin = compact ? new Thickness(4, 0) : new Thickness(24, 0);
            layout.ColumnDefinitions[1].Width = compact ? 238 : 320;
            layout.ColumnSpacing = compact ? 16 : 28;
            menu.Spacing = compact ? 8 : 12;
            panel.Padding = compact ? 16 : 24;
            foreach (var button in buttons) button.HeightRequest = compact ? 46 : 54;
        };
        SizeChanged += resizeMenu;
        resizeMenu(this, EventArgs.Empty);
    }

    private EventHandler? resizeMenu;
}
