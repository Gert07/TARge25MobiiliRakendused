using System.ComponentModel;
using System.Runtime.CompilerServices;
using Example_app.Services;
using Microsoft.Data.Sqlite;

namespace Example_app.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settings;
    private string _optionsLanguage = string.Empty;
    private bool _refreshing;
    public event PropertyChangedEventHandler? PropertyChanged;
    public string[] Languages { get; } = ["Eesti", "English"];
    public string[] Themes { get; private set; } = [];
    public string[] GameModes { get; private set; } = [];
    public string ErrorMessage { get; private set; } = string.Empty;

    public int LanguageIndex
    {
        get => _settings.Current.LanguageCode == "en" ? 1 : 0;
        set { if (!_refreshing && value is 0 or 1 && value != LanguageIndex) TrySave(() => _settings.SetLanguage(value == 1 ? "en" : "et")); }
    }

    public int ThemeIndex
    {
        get => _settings.Current.Theme == "Dark" ? 1 : 0;
        set { if (!_refreshing && value is 0 or 1 && value != ThemeIndex) TrySave(() => _settings.SetTheme(value == 1 ? "Dark" : "Light")); }
    }

    public int GameModeIndex
    {
        get => _settings.Current.GameMode;
        set { if (!_refreshing && value is 0 or 1 && value != GameModeIndex) TrySave(() => _settings.SetGameMode(value)); }
    }

    public SettingsViewModel(SettingsService settings)
    {
        _settings = settings;
        Refresh();
    }

    private void TrySave(Action save)
    {
        try
        {
            save();
            ErrorMessage = string.Empty;
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            ErrorMessage = LanguageService.Instance["SaveFailed"];
        }
        Refresh();
    }

    public void Activate()
    {
        _settings.Changed += Refresh;
        Refresh();
    }

    public void Deactivate() => _settings.Changed -= Refresh;
    private void Refresh()
    {
        _refreshing = true;
        try
        {
            if (_optionsLanguage != _settings.Current.LanguageCode)
            {
                _optionsLanguage = _settings.Current.LanguageCode;
                Themes = [LanguageService.Instance["LightTheme"], LanguageService.Instance["DarkTheme"]];
                GameModes = [LanguageService.Instance["TwoPlayers"], LanguageService.Instance["VsBot"]];
            }
            OnPropertyChanged(string.Empty);
        }
        finally { _refreshing = false; }
    }
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
