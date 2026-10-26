using AlienBreach.Core.Models;
using AlienBreach.Core.Persistence;

namespace AlienBreach.Services;

public sealed class SettingsService(IGameRepository repository)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public GameSettings Current { get; private set; } = new();
    public event EventHandler? Changed;

    public async Task LoadAsync()
    {
        await gate.WaitAsync();
        try
        {
            Current = await repository.GetSettingsAsync();
            L.SetLanguage(Current.Language);
        }
        finally { gate.Release(); }
    }

    public async Task SaveAsync(GameSettings settings)
    {
        settings.Validate();
        await gate.WaitAsync();
        try
        {
            await repository.SaveSettingsAsync(settings);
            Current = settings;
            L.SetLanguage(settings.Language);
            Changed?.Invoke(this, EventArgs.Empty);
        }
        finally { gate.Release(); }
    }
}
