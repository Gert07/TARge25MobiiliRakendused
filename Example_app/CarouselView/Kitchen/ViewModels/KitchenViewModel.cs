using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CarouselView.Kitchen.Localization;
using CarouselView.Kitchen.Models;

namespace CarouselView.Kitchen.ViewModels;

public sealed class KitchenViewModel : INotifyPropertyChanged
{
    private const string LanguagePreference = "Kitchen.Language";
    private int position;
    private bool isPlaying = true;
    private bool isLanguageMenuOpen;

    public KitchenLocalizer Strings { get; } = new();
    public ObservableCollection<Dish> Dishes { get; }
    public ICommand SetLanguageCommand { get; }
    public ICommand ToggleLanguageMenuCommand { get; }
    public string SelectedLanguageFlag => $"flag_{Strings.Culture.TwoLetterISOLanguageName}.png";
    public string LanguageMenuDescription => Strings[IsLanguageMenuOpen ? "CloseLanguages" : "ChooseLanguage"];
    public string LanguageMenuChevron => IsLanguageMenuOpen ? "⌃" : "⌄";

    public bool IsLanguageMenuOpen
    {
        get => isLanguageMenuOpen;
        set
        {
            if (isLanguageMenuOpen == value) return;
            isLanguageMenuOpen = value;
            Notify(nameof(IsLanguageMenuOpen));
            Notify(nameof(LanguageMenuDescription));
            Notify(nameof(LanguageMenuChevron));
        }
    }
    public bool IsEstonian => Strings.Culture.TwoLetterISOLanguageName == "et";
    public bool IsEnglish => Strings.Culture.TwoLetterISOLanguageName == "en";
    public bool IsItalian => Strings.Culture.TwoLetterISOLanguageName == "it";
    public string Counter => string.Format(Strings.Culture, Strings["CounterFormat"], Position + 1, Dishes.Count);
    public string PlaybackStatus => Strings[IsPlaying ? "AutoPlaying" : "AutoPaused"];
    public string PlaybackButton => Strings[IsPlaying ? "Pause" : "Play"];

    public int Position
    {
        get => position;
        set
        {
            if (value < 0 || value >= Dishes.Count || position == value) return;
            position = value;
            Notify(nameof(Position));
            Notify(nameof(Counter));
        }
    }

    public bool IsPlaying
    {
        get => isPlaying;
        set
        {
            if (isPlaying == value) return;
            isPlaying = value;
            Notify(nameof(IsPlaying));
            Notify(nameof(PlaybackStatus));
            Notify(nameof(PlaybackButton));
        }
    }

    public KitchenViewModel()
    {
        Strings.SetLanguage(Preferences.Default.Get(LanguagePreference, "et"));
        Dishes = new ObservableCollection<Dish>
        {
            new("Carbonara", "kitchen_carbonara.png", Strings),
            new("Pizza", "kitchen_margherita.png", Strings),
            new("Tiramisu", "kitchen_tiramisu.png", Strings),
            new("Risotto", "kitchen_risotto.png", Strings),
            new("Lasagne", "kitchen_lasagne.png", Strings)
        };
        SetLanguageCommand = new Command<string>(ChangeLanguage);
        ToggleLanguageMenuCommand = new Command(() => IsLanguageMenuOpen = !IsLanguageMenuOpen);
    }

    private void ChangeLanguage(string code)
    {
        if (code is not ("et" or "en" or "it")) return;
        IsLanguageMenuOpen = false;
        Strings.SetLanguage(code);
        Preferences.Default.Set(LanguagePreference, code);
        foreach (var dish in Dishes) dish.RefreshLocalization();
        Notify(null);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify(string? property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}
