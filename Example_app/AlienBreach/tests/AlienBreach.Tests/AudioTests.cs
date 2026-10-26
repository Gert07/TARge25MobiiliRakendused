using System.Text;
using AlienBreach.Core.Game;
using AlienBreach.Core.Maps;
using AlienBreach.Core.Models;
using AlienBreach.Core.Persistence;
using AlienBreach.Core.Turrets;
using AlienBreach.Services;

namespace AlienBreach.Tests;

[Collection("Settings")]
public sealed class AudioTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "alien-audio-" + Guid.NewGuid().ToString("N"));
    private SettingsService Settings() => new(new SqliteGameRepository(Path.Combine(directory, "game.db")));

    public static IEnumerable<object[]> Cues => Enum.GetValues<SoundCue>().Select(cue => new object[] { cue });

    [Theory]
    [MemberData(nameof(Cues))]
    public void EveryEffectIsShortNonClippingMonoPcm(SoundCue cue)
    {
        var bytes = SoundGenerator.Create(cue);
        using var reader = new BinaryReader(new MemoryStream(bytes));
        Assert.Equal("RIFF", Encoding.ASCII.GetString(reader.ReadBytes(4)));
        Assert.Equal(bytes.Length - 8, reader.ReadInt32());
        Assert.Equal("WAVEfmt ", Encoding.ASCII.GetString(reader.ReadBytes(8)));
        Assert.Equal(16, reader.ReadInt32());
        Assert.Equal(1, reader.ReadInt16());
        Assert.Equal(1, reader.ReadInt16());
        Assert.Equal(22050, reader.ReadInt32());
        Assert.Equal(44100, reader.ReadInt32());
        Assert.Equal(2, reader.ReadInt16());
        Assert.Equal(16, reader.ReadInt16());
        Assert.Equal("data", Encoding.ASCII.GetString(reader.ReadBytes(4)));
        Assert.Equal(bytes.Length - 44, reader.ReadInt32());
        var samples = new List<short>();
        while (reader.BaseStream.Position < bytes.Length) samples.Add(reader.ReadInt16());
        Assert.InRange(samples.Count / 22050.0, .04, .51);
        Assert.Equal(0, samples[0]);
        Assert.Equal(0, samples[^1]);
        Assert.Contains(samples, value => Math.Abs((int)value) > 500);
        Assert.All(samples, value => Assert.InRange((int)value, -16000, 16000));
    }

    [Fact]
    public void GameCuesFollowSuccessfulActionsAndFinishOnlyOnce()
    {
        var game = new GameEngine(PlayerProfile.New, MapCatalog.All[0]);
        var cues = new List<SoundCue>();
        game.SoundRequested += cues.Add;
        Assert.False(game.PlaceTurret(TurretType.Pulse, new(-1, -1)).Success);
        Assert.Empty(cues);
        var turret = game.PlaceTurret(TurretType.Pulse, new(100, 220)).Turret!;
        game.UpgradeTurret(turret);
        game.SellTurret(turret);
        Assert.Equal(new[] { SoundCue.Build, SoundCue.Build, SoundCue.Build }, cues);
        game.StartNextWave();
        game.StartNextWave();
        game.Retreat();
        game.Retreat();
        Assert.Equal(1, cues.Count(cue => cue == SoundCue.WaveStart));
        Assert.Equal(1, cues.Count(cue => cue == SoundCue.Retreat));
    }

    [Fact]
    public void CombatProducesShotsHitsAndWaveCompletion()
    {
        var game = new GameEngine(PlayerProfile.New, MapCatalog.All[0]);
        var cues = new List<SoundCue>();
        game.SoundRequested += cues.Add;
        game.PlaceTurret(TurretType.Pulse, new(100, 220));
        game.StartNextWave();
        for (var i = 0; i < 80; i++) game.Update(.1f);
        Assert.Contains(SoundCue.Shot, cues);
        Assert.Contains(SoundCue.Hit, cues);
        for (var i = 0; i < 400 && game.State == GameState.Running; i++)
        {
            foreach (var alien in game.Aliens) game.ResolveHit(alien, 100000, ShotKind.Plasma);
            game.Update(.1f);
        }
        Assert.Contains(SoundCue.Blast, cues);
        Assert.Equal(1, cues.Count(cue => cue == SoundCue.WaveCleared));
    }

    [Fact]
    public void BaseDamageAndDefeatProduceCues()
    {
        var map = MapCatalog.All[0] with { Path = new EnemyPath(new(0, 100), new(1, 100)) };
        var game = new GameEngine(PlayerProfile.New, map);
        var cues = new List<SoundCue>();
        game.SoundRequested += cues.Add;
        for (var i = 0; i < 1000 && game.State != GameState.Finished; i++)
        {
            if (game.State == GameState.Building) game.StartNextWave();
            game.Update(.1f);
        }
        Assert.Equal(GameState.Finished, game.State);
        Assert.Contains(SoundCue.BaseHit, cues);
        Assert.Equal(SoundCue.Defeat, cues[^1]);
    }

    [Fact]
    public async Task RepeatedCombatSoundsAreCoalescedButOtherCuesCanOverlap()
    {
        var playback = new FakePlayback();
        var clock = new ManualClock();
        using var audio = new GameAudioService(Settings(), playback, clock);
        await audio.InitializeAsync();
        await audio.InitializeAsync();
        Assert.Equal(1, playback.Initializations);
        audio.Play(SoundCue.Shot); // Menus are silent.
        Assert.Empty(playback.Played);
        audio.Resume();
        audio.Play(SoundCue.Shot);
        audio.Play(SoundCue.Shot);
        audio.Play(SoundCue.Hit);
        clock.Advance(80);
        audio.Play(SoundCue.Shot);
        Assert.Equal(new[] { SoundCue.Shot, SoundCue.Hit, SoundCue.Shot }, playback.Played);
    }

    [Fact]
    public async Task PauseMuteResumeAndDisposeControlPlaybackImmediately()
    {
        var playback = new FakePlayback();
        var settings = Settings();
        using var audio = new GameAudioService(settings, playback, TimeProvider.System);
        await audio.InitializeAsync();
        audio.Resume();
        audio.Play(SoundCue.Build);
        audio.Suspend();
        var stopsAfterPause = playback.Stops;
        audio.Play(SoundCue.WaveStart);
        Assert.Single(playback.Played);
        audio.Resume();
        await settings.SaveAsync(new(SoundEnabled: false));
        Assert.True(playback.Stops > stopsAfterPause);
        audio.Play(SoundCue.WaveStart);
        Assert.Single(playback.Played);
        await settings.SaveAsync(new(SoundEnabled: true));
        audio.Play(SoundCue.Defeat);
        Assert.Equal(SoundCue.Defeat, playback.Played[^1]);
        audio.Dispose();
        audio.Resume();
        audio.Play(SoundCue.Build);
        Assert.Equal(2, playback.Played.Count);
        Assert.True(playback.Disposed);
    }

    [Fact]
    public async Task AudioDriverFailuresDoNotInterruptTheGame()
    {
        var playback = new FakePlayback { FailInitialize = true };
        using var audio = new GameAudioService(Settings(), playback, TimeProvider.System);
        await audio.InitializeAsync();
        audio.Resume();
        audio.Play(SoundCue.Shot);
        Assert.Empty(playback.Played);
        var failingPlayer = new FakePlayback { FailPlay = true };
        using var other = new GameAudioService(Settings(), failingPlayer, TimeProvider.System);
        await other.InitializeAsync();
        other.Resume();
        other.Play(SoundCue.Shot);
    }

    public void Dispose()
    {
        L.SetLanguage("et");
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    private sealed class ManualClock : TimeProvider
    {
        private long timestamp;
        public override long TimestampFrequency => 1000;
        public override long GetTimestamp() => timestamp;
        public void Advance(long milliseconds) => timestamp += milliseconds;
    }

    private sealed class FakePlayback : IAudioPlayback
    {
        public List<SoundCue> Played { get; } = [];
        public int Initializations { get; private set; }
        public int Stops { get; private set; }
        public bool Disposed { get; private set; }
        public bool FailInitialize { get; init; }
        public bool FailPlay { get; init; }
        public Task InitializeAsync(IReadOnlyDictionary<SoundCue, byte[]> sounds)
        {
            Initializations++;
            return FailInitialize ? Task.FromException(new IOException("No audio device")) : Task.CompletedTask;
        }
        public void Play(SoundCue cue)
        {
            if (FailPlay) throw new IOException("Audio device disconnected");
            Played.Add(cue);
        }
        public void StopAll() => Stops++;
        public void Dispose() => Disposed = true;
    }
}
