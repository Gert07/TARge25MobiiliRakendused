using System.Globalization;
using System.Resources;
using AlienBreach.Core.Models;
using AlienBreach.Core.Persistence;
using AlienBreach.Services;
using AlienBreach.ViewModels;
using Microsoft.Data.Sqlite;

namespace AlienBreach.Tests;

[Collection("Settings")]
public sealed class SettingsTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "alien-settings-" + Guid.NewGuid().ToString("N"));
    private string Database => Path.Combine(directory, "game.db");
    private SqliteGameRepository Repository() => new(Database);

    [Fact]
    public async Task FreshDatabaseStartsInEstonianAtNormalSpeed()
    {
        Assert.Equal(new GameSettings("et", 1), await Repository().GetSettingsAsync());
    }

    [Theory]
    [InlineData("et", 2)]
    [InlineData("en", 1)]
    [InlineData("en", 2)]
    public async Task ChangedSettingsAreRestoredOnNextLaunch(string language, int speed)
    {
        var service = new SettingsService(Repository());
        await service.LoadAsync();
        await service.SaveAsync(new(language, speed));
        var reopened = new SettingsService(Repository());
        await reopened.LoadAsync();
        Assert.Equal(new GameSettings(language, speed), reopened.Current);
        Assert.Equal(language, L.Culture.Name);
        Assert.Equal(language == "et" ? "Sätted" : "Settings", L.T("Settings"));
    }

    [Theory]
    [InlineData("fr", 1)]
    [InlineData("et", 0)]
    [InlineData("en", 3)]
    public async Task InvalidSettingsDoNotReplaceExistingValues(string language, int speed)
    {
        var repository = Repository();
        await repository.SaveSettingsAsync(new("en", 2));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.SaveSettingsAsync(new(language, speed)));
        Assert.Equal(new GameSettings("en", 2), await Repository().GetSettingsAsync());
    }

    [Fact]
    public async Task VersionTwoMigrationKeepsProgressHistoryAndMapRecords()
    {
        var repository = Repository();
        var run = new RunResult("before-settings", DateTimeOffset.UtcNow, 6, 42, 123, 70, "Retreated", "outpost-07");
        await repository.SaveRunAsync(run);
        using (var connection = new SqliteConnection($"Data Source={Database};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DROP TABLE Settings; PRAGMA user_version = 2;";
            command.ExecuteNonQuery();
        }
        var migrated = Repository();
        Assert.Equal(new GameSettings(), await migrated.GetSettingsAsync());
        Assert.Equal(123, (await migrated.GetProfileAsync()).Credits);
        Assert.Equal(run, Assert.Single(await migrated.GetRecentRunsAsync()));
        Assert.Equal(new MapRecord("outpost-07", 6, 1, 0, 42), Assert.Single(await migrated.GetMapRecordsAsync()));
        await migrated.SaveSettingsAsync(new("en", 2));
        Assert.Equal(new GameSettings("en", 2), await Repository().GetSettingsAsync());
    }

    [Fact]
    public async Task FailedSaveKeepsLanguageSpeedAndDatabaseConsistentAndCanBeRetried()
    {
        var repository = new FailingRepository(Repository());
        var settings = new SettingsService(repository);
        await settings.LoadAsync();
        var viewModel = new SettingsViewModel(settings);
        repository.FailSave = true;
        await viewModel.ChangeAsync(1, 1);
        Assert.False(viewModel.IsBusy);
        Assert.Equal(0, viewModel.LanguageIndex);
        Assert.Equal(0, viewModel.SpeedIndex);
        Assert.Equal("et", L.Culture.Name);
        Assert.Equal(L.T("SettingsSaveFailed"), viewModel.Status);
        Assert.Equal(new GameSettings(), await Repository().GetSettingsAsync());
        repository.FailSave = false;
        await viewModel.ChangeAsync(1, 1);
        Assert.Equal(new GameSettings("en", 2), await Repository().GetSettingsAsync());
        Assert.Equal("Settings saved.", viewModel.Status);
        Assert.Equal(1, viewModel.LanguageIndex);
        Assert.Equal(1, viewModel.SpeedIndex);
    }

    [Fact]
    public void BothLanguagesCoverAllResourcesAndFormatArguments()
    {
        var manager = new ResourceManager("AlienBreach.Resources.Localization.AppResources", typeof(L).Assembly);
        using var english = manager.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
        using var estonian = manager.GetResourceSet(CultureInfo.GetCultureInfo("et"), true, false)!;
        Assert.NotNull(english);
        Assert.NotNull(estonian);
        var englishKeys = english.Cast<System.Collections.DictionaryEntry>().Select(entry => (string)entry.Key).Order().ToArray();
        var estonianKeys = estonian.Cast<System.Collections.DictionaryEntry>().Select(entry => (string)entry.Key).Order().ToArray();
        Assert.Equal(englishKeys, estonianKeys);
        foreach (var key in englishKeys)
        {
            var en = english.GetString(key)!;
            var et = estonian.GetString(key)!;
            Assert.False(string.IsNullOrWhiteSpace(et));
            Assert.Equal(System.Text.CompositeFormat.Parse(en).MinimumArgumentCount,
                System.Text.CompositeFormat.Parse(et).MinimumArgumentCount);
        }
        L.SetLanguage("et");
        Assert.Equal("LAINE 03", L.F("WaveFormat", 3));
        Assert.Equal("Eelpost 07", L.MapName("outpost-07"));
        Assert.Equal("Varasem ellujäämismäng", L.MapName("old-map"));
        Assert.Equal("BAAS LANGES", L.Outcome("Overrun"));
        L.SetLanguage("en");
        Assert.Equal("WAVE 03", L.F("WaveFormat", 3));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SoundChoiceSurvivesRestartAndOtherSettingsChanges(bool enabled)
    {
        var settings = new SettingsService(Repository());
        await settings.LoadAsync();
        var viewModel = new SettingsViewModel(settings);
        await viewModel.ChangeAsync(0, 0, enabled);
        await viewModel.ChangeAsync(1, 1);
        var reopened = new SettingsService(Repository());
        await reopened.LoadAsync();
        Assert.Equal(new GameSettings("en", 2, enabled), reopened.Current);
    }

    [Fact]
    public async Task VersionThreeMigrationKeepsLanguageSpeedAndProgress()
    {
        var repository = Repository();
        await repository.SaveRunAsync(new("old-sound", DateTimeOffset.UtcNow, 3, 10, 75, 40, "Retreated", "outpost-07"));
        using (var connection = new SqliteConnection($"Data Source={Database};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                DROP TABLE Settings;
                CREATE TABLE Settings (Id INTEGER PRIMARY KEY, Language TEXT NOT NULL, GameSpeed INTEGER NOT NULL);
                INSERT INTO Settings VALUES (1, 'en', 2);
                PRAGMA user_version = 3;
                """;
            command.ExecuteNonQuery();
        }
        Assert.Equal(new GameSettings("en", 2, true), await Repository().GetSettingsAsync());
        Assert.Equal(75, (await Repository().GetProfileAsync()).Credits);
        await Repository().SaveSettingsAsync(new("en", 2, false));
        Assert.False((await Repository().GetSettingsAsync()).SoundEnabled);
    }

    [Fact]
    public async Task FailedSoundSaveKeepsPreviousSwitchValue()
    {
        var repository = new FailingRepository(Repository()) { FailSave = true };
        var settings = new SettingsService(repository);
        await settings.LoadAsync();
        var viewModel = new SettingsViewModel(settings);
        await viewModel.ChangeAsync(0, 0, false);
        Assert.True(viewModel.SoundEnabled);
        Assert.True((await Repository().GetSettingsAsync()).SoundEnabled);
        Assert.Equal(L.T("SettingsSaveFailed"), viewModel.Status);
    }

    public void Dispose()
    {
        L.SetLanguage("et");
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    private sealed class FailingRepository(IGameRepository inner) : IGameRepository
    {
        public bool FailSave { get; set; }
        public Task<GameSettings> GetSettingsAsync() => inner.GetSettingsAsync();
        public Task SaveSettingsAsync(GameSettings settings) => FailSave
            ? Task.FromException(new IOException("Simulated database write failure")) : inner.SaveSettingsAsync(settings);
        public Task<PlayerProfile> GetProfileAsync() => inner.GetProfileAsync();
        public Task<IReadOnlyList<RunResult>> GetRecentRunsAsync(int limit = 12) => inner.GetRecentRunsAsync(limit);
        public Task<IReadOnlyList<MapRecord>> GetMapRecordsAsync() => inner.GetMapRecordsAsync();
        public Task SaveRunAsync(RunResult result) => inner.SaveRunAsync(result);
        public Task<PurchaseResult> PurchaseUpgradeAsync(UpgradeType type) => inner.PurchaseUpgradeAsync(type);
    }
}
