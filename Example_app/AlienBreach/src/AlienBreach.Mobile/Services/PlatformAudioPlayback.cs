using AlienBreach.Core.Game;

namespace AlienBreach.Services;

public sealed class PlatformAudioPlayback : IAudioPlayback
{
#if WINDOWS
    private readonly Dictionary<SoundCue, Windows.Media.Playback.MediaPlayer> players = [];
#elif ANDROID
    private Android.Media.SoundPool? pool;
    private readonly Dictionary<SoundCue, int> sounds = [];
    private readonly Queue<int> streams = [];
#elif IOS || MACCATALYST
    private readonly Dictionary<SoundCue, AVFoundation.AVAudioPlayer> players = [];
#endif

    public async Task InitializeAsync(IReadOnlyDictionary<SoundCue, byte[]> samples)
    {
#if ANDROID
        using var attributes = new Android.Media.AudioAttributes.Builder()
            .SetUsage(Android.Media.AudioUsageKind.Game)!
            .SetContentType(Android.Media.AudioContentType.Sonification)!.Build()!;
        using var builder = new Android.Media.SoundPool.Builder();
        pool = builder.SetMaxStreams(8)!.SetAudioAttributes(attributes)!.Build()!;
#endif
        var directory = Path.Combine(FileSystem.CacheDirectory, "sounds-v1");
        Directory.CreateDirectory(directory);
        foreach (var (cue, bytes) in samples)
        {
            var path = Path.Combine(directory, cue + ".wav");
            await File.WriteAllBytesAsync(path, bytes);
#if WINDOWS
            var player = new Windows.Media.Playback.MediaPlayer
            {
                Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(path)),
                Volume = .65,
                AudioCategory = Windows.Media.Playback.MediaPlayerAudioCategory.GameEffects
            };
            player.CommandManager.IsEnabled = false;
            players.Add(cue, player);
#elif ANDROID
            var id = await pool.LoadAsync(path, 1);
            if (id == 0) throw new IOException($"Could not load sound {cue}.");
            sounds.Add(cue, id);
#elif IOS || MACCATALYST
            using var data = Foundation.NSData.FromArray(bytes);
            var player = AVFoundation.AVAudioPlayer.FromData(data)
                ?? throw new IOException($"Could not load sound {cue}.");
            player.Volume = .65f;
            player.PrepareToPlay();
            players.Add(cue, player);
#endif
        }
    }

    public void Play(SoundCue cue)
    {
#if WINDOWS
        if (!players.TryGetValue(cue, out var player)) return;
        player.Pause();
        player.PlaybackSession.Position = TimeSpan.Zero;
        player.Play();
#elif ANDROID
        if (pool is null || !sounds.TryGetValue(cue, out var id)) return;
        // Keep the last eight handles so muting/pausing can stop all audible streams.
        if (streams.Count >= 8) pool.Stop(streams.Dequeue());
        var playing = pool.Play(id, .65f, .65f, cue is SoundCue.Shot or SoundCue.Hit ? 1 : 2, 0, 1);
        if (playing != 0) streams.Enqueue(playing);
#elif IOS || MACCATALYST
        if (!players.TryGetValue(cue, out var player)) return;
        player.Stop();
        player.CurrentTime = 0;
        player.Play();
#endif
    }

    public void StopAll()
    {
#if WINDOWS
        foreach (var player in players.Values) player.Pause();
#elif ANDROID
        if (pool is null) return;
        foreach (var stream in streams) pool.Stop(stream);
        streams.Clear();
#elif IOS || MACCATALYST
        foreach (var player in players.Values) player.Stop();
#endif
    }

    public void Dispose()
    {
        StopAll();
#if WINDOWS
        foreach (var player in players.Values)
        {
            var source = player.Source as Windows.Media.Core.MediaSource;
            player.Source = null;
            player.Dispose();
            source?.Dispose();
        }
        players.Clear();
#elif ANDROID
        pool?.Release();
        pool?.Dispose();
        pool = null;
        sounds.Clear();
#elif IOS || MACCATALYST
        foreach (var player in players.Values) player.Dispose();
        players.Clear();
#endif
    }
}
