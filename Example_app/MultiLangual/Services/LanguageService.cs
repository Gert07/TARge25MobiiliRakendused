using MultiLangual.Resources.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MultiLangual.Services
{
    public static class LanguageService
    {
        public static event Action? LanguageChanged;

        public static void ChangeLanguage(string languageCode)
        {
            // Set the new language code in your application settings or preferences
            // For example, you can use Preferences.Default.Set("LanguageCode", languageCode);
            // Notify subscribers that the language has changed
            var culture = new CultureInfo(languageCode);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            AppResources.Culture = culture;
            LanguageChanged?.Invoke();
        }
    }
}
