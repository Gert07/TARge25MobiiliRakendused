using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Example_app.Multilangual.Resources.Localization;
using Example_app.Multilangual.Services;

namespace Example_app.Multilangual.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Greeting => AppResources.GreetingButton;
    public string ChangeLanguageLabel => AppResources.ChangeLanguage;
    public string EnglishButton => AppResources.EnglishButton;
    public string EstonianButton => AppResources.EstonianButton;
    public string RussianButton => AppResources.RussianButton;

    public ICommand SetEnglishCommand { get; }
    public ICommand SetEstonianCommand { get; }
    public ICommand SetRussianCommand { get; }

    public MainViewModel()
    {
        SetEnglishCommand = new Command(() => ChangeLanguage("en"));
        SetEstonianCommand = new Command(() => ChangeLanguage("et"));
        SetRussianCommand = new Command(() => ChangeLanguage("ru"));
    }

    private void ChangeLanguage(string languageCode)
    {
        LanguageService.ChangeLanguage(languageCode);
        OnPropertyChanged(string.Empty);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
