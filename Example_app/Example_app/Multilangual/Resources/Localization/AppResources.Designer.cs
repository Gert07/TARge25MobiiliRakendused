using System.Globalization;
using System.Resources;

#nullable enable

namespace Example_app.Multilangual.Resources.Localization;

internal static class AppResources
{
    private static readonly ResourceManager ResourceManagerInstance = new(
        "Example_app.Multilangual.Resources.Localization.AppResources",
        typeof(AppResources).Assembly);

    internal static CultureInfo? Culture { get; set; }

    internal static string GreetingButton => GetString(nameof(GreetingButton));
    internal static string ChangeLanguage => GetString(nameof(ChangeLanguage));
    internal static string EnglishButton => GetString(nameof(EnglishButton));
    internal static string EstonianButton => GetString(nameof(EstonianButton));
    internal static string RussianButton => GetString(nameof(RussianButton));

    private static string GetString(string name) =>
        ResourceManagerInstance.GetString(name, Culture) ?? name;
}
