using AlienBreach.Core.Game;

namespace AlienBreach.Services;

public interface IAudioPlayback : IDisposable
{
    Task InitializeAsync(IReadOnlyDictionary<SoundCue, byte[]> sounds);
    void Play(SoundCue cue);
    void StopAll();
}
