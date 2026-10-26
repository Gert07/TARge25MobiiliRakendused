using System.Diagnostics;
using AlienBreach.Core.Game;

namespace AlienBreach.Services;

public sealed class GameAudioService : IDisposable
{
    private readonly SettingsService settings;
    private readonly IAudioPlayback playback;
    private readonly TimeProvider clock;
    private readonly Dictionary<SoundCue, long> lastPlayed = [];
    private Task? initialization;
    private bool active;
    private bool ready;
    private bool disposed;

    public GameAudioService(SettingsService settings, IAudioPlayback playback, TimeProvider clock)
    {
        this.settings = settings;
        this.playback = playback;
        this.clock = clock;
        settings.Changed += SettingsChanged;
    }

    public Task InitializeAsync() => initialization ??= InitializeCoreAsync();

    private async Task InitializeCoreAsync()
    {
        try
        {
            var sounds = await Task.Run(() => Enum.GetValues<SoundCue>().ToDictionary(cue => cue, SoundGenerator.Create));
            await playback.InitializeAsync(sounds);
            ready = true;
        }
        catch (Exception exception)
        {
            // Audio hardware/driver failures must not prevent the game from opening.
            Debug.WriteLine($"Audio initialization failed: {exception.Message}");
        }
    }

    public void Resume()
    {
        if (disposed) return;
        lastPlayed.Clear();
        active = true;
    }

    public void Play(SoundCue cue)
    {
        if (!ready || disposed || !active || !settings.Current.SoundEnabled) return;
        var now = clock.GetTimestamp();
        var interval = cue switch
        {
            SoundCue.Shot => .075, SoundCue.Hit => .09, SoundCue.Blast => .12,
            SoundCue.BaseHit => .15, _ => .04
        };
        if (lastPlayed.TryGetValue(cue, out var previous) && clock.GetElapsedTime(previous, now).TotalSeconds < interval) return;
        lastPlayed[cue] = now;
        try
        {
            if (cue is SoundCue.Defeat or SoundCue.Retreat) playback.StopAll();
            playback.Play(cue);
        }
        catch (Exception exception) { Debug.WriteLine($"Audio playback failed: {exception.Message}"); }
    }

    public void Suspend()
    {
        active = false;
        StopAll();
    }

    private void SettingsChanged(object? sender, EventArgs args)
    {
        if (!settings.Current.SoundEnabled) StopAll();
    }

    private void StopAll()
    {
        if (disposed) return;
        try { playback.StopAll(); }
        catch (Exception exception) { Debug.WriteLine($"Audio stop failed: {exception.Message}"); }
    }

    public void Dispose()
    {
        if (disposed) return;
        Suspend();
        disposed = true;
        settings.Changed -= SettingsChanged;
        playback.Dispose();
    }
}
