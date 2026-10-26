using AlienBreach.Core.Persistence;
using AlienBreach.Views;
using AlienBreach.Services;

namespace AlienBreach;

public partial class App : Application
{
    private readonly IGameRepository repository;
    private readonly SettingsService settings;
    private readonly GameAudioService audio;
    public App(IGameRepository repository, SettingsService settings, GameAudioService audio)
    {
        InitializeComponent();
        this.repository = repository;
        this.settings = settings;
        this.audio = audio;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new NavigationPage(new StartupPage(repository, settings, audio))
        {
            BarBackgroundColor = Color.FromArgb("#07131A"), BarTextColor = Colors.White
        });
        window.Title = "Alien Breach";
        window.Width = 1120;
        window.Height = 720;
        window.MinimumWidth = 640;
        window.MinimumHeight = 380;
        window.Deactivated += (_, _) => Pause(window);
        window.Stopped += (_, _) => Pause(window);
        window.Destroying += (_, _) => audio.Dispose();
        return window;
    }

    private void Pause(Window window)
    {
        audio.Suspend();
        if (window.Page is NavigationPage navigation)
        {
            var current = navigation.Navigation.ModalStack.LastOrDefault() ?? navigation.CurrentPage;
            if (current is MissionPage mission) mission.PauseForBackground();
        }
    }
}
