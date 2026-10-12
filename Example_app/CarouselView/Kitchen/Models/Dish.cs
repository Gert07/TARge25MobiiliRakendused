using System.ComponentModel;
using CarouselView.Kitchen.Localization;

namespace CarouselView.Kitchen.Models;

public sealed class Dish(string key, string image, KitchenLocalizer strings) : INotifyPropertyChanged
{
    public string Image { get; } = image;
    public string Title => strings[$"{key}Title"];
    public string Description => strings[$"{key}Description"];
    public string Region => strings[$"{key}Region"];
    public string Category => strings[$"{key}Category"];
    public string Ingredients => strings[$"{key}Ingredients"];
    public string Preparation => strings[$"{key}Preparation"];
    public string Tip => strings[$"{key}Tip"];
    public string DetailsLabel => strings["Details"];

    public event PropertyChangedEventHandler? PropertyChanged;
    public void RefreshLocalization() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
}
