using Example_app.Models;

namespace Example_app.Services;

public sealed class SettingsService
{
    private readonly AppDatabase _database;
    public AppSettings Current { get; private set; }
    public event Action? Changed;

    public SettingsService(AppDatabase database)
    {
        _database = database;
        Current = database.LoadSettings();
        LanguageService.Instance.ApplyLanguage(Current.LanguageCode);
    }

    public void SetLanguage(string code) => Save(Current with { LanguageCode = code });
    public void SetTheme(string theme) => Save(Current with { Theme = theme });
    public void SetGameMode(int mode) => Save(Current with { GameMode = mode });
    public void SetWelcomeSeen(bool seen) => Save(Current with { HasSeenWelcome = seen });

    private void Save(AppSettings settings)
    {
        if (settings == Current) return;
        _database.SaveSettings(settings);
        bool languageChanged = settings.LanguageCode != Current.LanguageCode;
        Current = settings;
        if (languageChanged) LanguageService.Instance.ApplyLanguage(settings.LanguageCode);
        ApplyTheme();
        Changed?.Invoke();
    }

    public void ApplyTheme()
    {
        if (Application.Current is { } application)
            application.UserAppTheme = Current.Theme == "Dark" ? AppTheme.Dark : AppTheme.Light;
    }
}
