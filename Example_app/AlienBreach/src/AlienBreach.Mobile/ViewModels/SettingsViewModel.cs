using System.ComponentModel;
using AlienBreach.Models;
using AlienBreach.Services;

namespace AlienBreach.ViewModels;

public sealed class SettingsViewModel(SettingsService settings) : INotifyPropertyChanged
{
    public IReadOnlyList<LanguageOption> Languages => LanguageOption.All;
    public int LanguageIndex => settings.Current.Language == "et" ? 0 : 1;
    public int SpeedIndex => settings.Current.GameSpeed - 1;
    public bool SoundEnabled => settings.Current.SoundEnabled;
    public bool IsBusy { get; private set; }
    public string Status { get; private set; } = "";
    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task ChangeAsync(int languageIndex, int speedIndex, bool? soundEnabled = null)
    {
        if (IsBusy || languageIndex < 0 || speedIndex < 0) return;
        IsBusy = true;
        Status = L.T("SavingSettings");
        Refresh();
        try
        {
            await settings.SaveAsync(new(Languages[languageIndex].Code, speedIndex + 1, soundEnabled ?? SoundEnabled));
            Status = L.T("SettingsSaved");
        }
        catch (Exception)
        {
            // The controls return to the last successfully persisted values.
            Status = L.T("SettingsSaveFailed");
        }
        finally { IsBusy = false; Refresh(); }
    }

    private void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
}
