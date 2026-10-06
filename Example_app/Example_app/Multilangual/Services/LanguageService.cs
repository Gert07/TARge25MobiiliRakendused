using System.Globalization;
using Example_app.Multilangual.Resources.Localization;

namespace Example_app.Multilangual.Services;

public static class LanguageService
{
    public static event Action? LanguageChanged;

    public static void ChangeLanguage(string languageCode)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(languageCode);

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        AppResources.Culture = culture;

        LanguageChanged?.Invoke();
    }
}
