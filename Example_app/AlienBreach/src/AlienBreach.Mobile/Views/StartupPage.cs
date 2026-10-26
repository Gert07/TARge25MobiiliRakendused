using AlienBreach.Core.Persistence;
using AlienBreach.Services;

namespace AlienBreach.Views;

// Never show the menu or allow changes until saved settings have been loaded.
public sealed class StartupPage : ContentPage
{
    private readonly IGameRepository repository;
    private readonly SettingsService settings;
    private readonly GameAudioService audio;
    private readonly Label status = Theme.Text(L.T("Starting"), 18);
    private readonly Button retry = Theme.Action(L.T("Retry"));
    private bool loading;

    public StartupPage(IGameRepository repository, SettingsService settings, GameAudioService audio)
    {
        this.repository = repository;
        this.settings = settings;
        this.audio = audio;
        BackgroundColor = Color.FromArgb("#07131A");
        NavigationPage.SetHasNavigationBar(this, false);
        retry.IsVisible = false;
        retry.Clicked += async (_, _) => await LoadAsync();
        Content = new VerticalStackLayout
        {
            Padding = 24, Spacing = 16, VerticalOptions = LayoutOptions.Center,
            Children = { status, retry }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (loading) return;
        loading = true;
        retry.IsVisible = false;
        status.Text = L.T("Starting");
        try
        {
            await settings.LoadAsync();
            await audio.InitializeAsync();
            Navigation.InsertPageBefore(new HangarPage(repository, settings, audio), this);
            await Navigation.PopAsync(false);
        }
        catch (Exception)
        {
            status.Text = L.T("StartupFailed");
            retry.Text = L.T("Retry");
            retry.IsVisible = true;
        }
        finally { loading = false; }
    }
}
