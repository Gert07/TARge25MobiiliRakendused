using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace CarouselView.Kitchen.Localization;

public sealed class KitchenLocalizer : INotifyPropertyChanged
{
    private static readonly ResourceManager Resources = new(
        "CarouselView.Resources.Localization.KitchenResources", typeof(KitchenLocalizer).Assembly);

    public CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("et");
    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => Resources.GetString(key, Culture)
        ?? throw new MissingManifestResourceException($"Missing kitchen translation: {key}");

    public void SetLanguage(string code)
    {
        // Explicit resource culture keeps this gallery independent of the other exercises.
        Culture = CultureInfo.GetCultureInfo(code is "et" or "en" or "it" ? code : "et");
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
