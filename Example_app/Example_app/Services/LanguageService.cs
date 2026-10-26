using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Example_app.Services;

public sealed class LanguageService : INotifyPropertyChanged
{
    public static LanguageService Instance { get; } = new();
    private readonly ResourceManager _resources = new(
        "Example_app.Resources.Localization.AppResources", typeof(LanguageService).Assembly);

    public event PropertyChangedEventHandler? PropertyChanged;
    public string this[string key] => _resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    private LanguageService() { }

    public void ApplyLanguage(string languageCode)
    {
        var culture = CultureInfo.GetCultureInfo(languageCode);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
    }

    public string Format(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, this[key], args);
}
