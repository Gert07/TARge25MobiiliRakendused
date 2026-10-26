using System.Globalization;
using System.Resources;

namespace AlienBreach.Services;

public static class L
{
    private static readonly ResourceManager Resources = new("AlienBreach.Resources.Localization.AppResources", typeof(L).Assembly);
    public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("et");

    public static void SetLanguage(string language)
    {
        if (language is not ("et" or "en")) throw new ArgumentOutOfRangeException(nameof(language));
        Culture = CultureInfo.GetCultureInfo(language);
        CultureInfo.DefaultThreadCurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.CurrentUICulture = Culture;
    }

    public static string T(string key) => Resources.GetString(key, Culture)
        ?? throw new MissingManifestResourceException($"Missing translation: {key}");
    public static string F(string key, params object[] args) => string.Format(Culture, T(key), args);

    public static string MapName(string id) => T("Map_" +
        (Core.Maps.MapCatalog.All.Any(map => map.Id == id) ? id : "legacy-survival"));

    public static string Outcome(string outcome) => T("Outcome_" +
        (outcome is "Overrun" or "Retreated" or "Victory" or "Extracted" ? outcome : "Unknown"));
}
