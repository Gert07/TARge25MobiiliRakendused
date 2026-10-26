using AlienBreach.Services;
using AlienBreach.ViewModels;

namespace AlienBreach.Views;

public sealed class SettingsPage : HudPage
{
    private readonly SettingsViewModel viewModel;
    private readonly Picker language = new() { TextColor = Colors.White, TitleColor = Colors.White };
    private readonly Picker speed = new() { TextColor = Colors.White, TitleColor = Colors.White };
    private readonly Label languageLabel = Theme.Text("", 16);
    private readonly Label speedLabel = Theme.Text("", 16);
    private readonly Label soundLabel = Theme.Text("", 16);
    private readonly Switch sound = new() { OnColor = Theme.Mint, HorizontalOptions = LayoutOptions.End };
    private readonly Label hint = Theme.Text("", 12, Theme.Muted);
    private readonly Label status = Theme.Text("", 12, Theme.Amber);
    private bool refreshing;

    public SettingsPage(SettingsService settings) : base(L.T("Settings"))
    {
        viewModel = new SettingsViewModel(settings);
        BindingContext = viewModel;
        language.ItemsSource = viewModel.Languages.Select(option => option.Name).ToList();
        speed.ItemsSource = new[] { "1×", "2×" };
        var soundRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }
        };
        soundLabel.VerticalOptions = LayoutOptions.Center;
        soundRow.Add(soundLabel, 0);
        soundRow.Add(sound, 1);
        var form = new VerticalStackLayout
        {
            Spacing = 10,
            Children = { languageLabel, language, speedLabel, speed, soundRow, hint, status }
        };
        var panel = Theme.Card(form, 20);
        panel.MaximumWidthRequest = 520;
        panel.HorizontalOptions = LayoutOptions.Fill;
        panel.VerticalOptions = LayoutOptions.Center;
        Body.Content = new ScrollView { Content = panel };
        Refresh();
        viewModel.PropertyChanged += (_, _) => Refresh();
        language.SelectedIndexChanged += async (_, _) => await SaveAsync();
        speed.SelectedIndexChanged += async (_, _) => await SaveAsync();
        sound.Toggled += async (_, _) => await SaveAsync();
    }

    private async Task SaveAsync()
    {
        if (!refreshing) await viewModel.ChangeAsync(language.SelectedIndex, speed.SelectedIndex, sound.IsToggled);
    }

    private void Refresh()
    {
        refreshing = true;
        try
        {
            SetHeading(L.T("Settings"));
            languageLabel.Text = language.Title = L.T("Language");
            speedLabel.Text = speed.Title = L.T("GameSpeed");
            soundLabel.Text = L.T("SoundEffects");
            SemanticProperties.SetDescription(sound, L.T("SoundEffects"));
            hint.Text = L.T("SettingsHint");
            status.Text = viewModel.Status;
            language.SelectedIndex = viewModel.LanguageIndex;
            speed.SelectedIndex = viewModel.SpeedIndex;
            sound.IsToggled = viewModel.SoundEnabled;
            language.IsEnabled = speed.IsEnabled = sound.IsEnabled = !viewModel.IsBusy;
        }
        finally { refreshing = false; }
    }
}
