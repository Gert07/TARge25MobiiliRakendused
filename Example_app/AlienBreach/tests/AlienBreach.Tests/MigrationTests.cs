using AlienBreach.Core.Models;
using AlienBreach.Core.Persistence;
using Microsoft.Data.Sqlite;

namespace AlienBreach.Tests;

public sealed class MigrationTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "alien-breach-migration-" + Guid.NewGuid().ToString("N"));
    private string Database => Path.Combine(directory, "game.db");

    [Fact]
    public async Task VersionOneShooterDataMigratesWithoutLosingProgress()
    {
        Directory.CreateDirectory(directory);
        SQLitePCL.Batteries_V2.Init();
        using (var connection = new SqliteConnection($"Data Source={Database};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE PlayerProfile (Id INTEGER PRIMARY KEY, Credits INTEGER, TotalKills INTEGER,
                    BestWave INTEGER, Runs INTEGER, RifleLevel INTEGER, ArmorLevel INTEGER);
                INSERT INTO PlayerProfile VALUES (1, 420, 72, 8, 4, 2, 3);
                CREATE TABLE Runs (Id TEXT PRIMARY KEY, FinishedAt TEXT, Wave INTEGER, Kills INTEGER,
                    Credits INTEGER, Seconds REAL, Outcome TEXT);
                INSERT INTO Runs VALUES ('old-run', '2026-10-13T10:00:00.0000000+00:00', 8, 72, 420, 260, 'Extracted');
                PRAGMA user_version = 1;
                """;
            command.ExecuteNonQuery();
        }
        var repository = new SqliteGameRepository(Database);
        Assert.Equal(new PlayerProfile(420, 72, 8, 4, 2, 3), await repository.GetProfileAsync());
        var oldRun = Assert.Single(await repository.GetRecentRunsAsync());
        Assert.Equal("legacy-survival", oldRun.MapId);
        Assert.Equal("Extracted", oldRun.Outcome);
        var newRun = new RunResult("new-run", DateTimeOffset.UtcNow, 15, 300, 100, 600, "Victory", "reactor-ring");
        await repository.SaveRunAsync(newRun);
        var reopened = new SqliteGameRepository(Database);
        Assert.Equal(520, (await reopened.GetProfileAsync()).Credits);
        Assert.Equal(2, (await reopened.GetProfileAsync()).FirepowerLevel);
        Assert.Equal(3, (await reopened.GetProfileAsync()).FortificationLevel);
        Assert.Equal(2, (await reopened.GetRecentRunsAsync()).Count);
        Assert.Equal(1, Assert.Single(await reopened.GetMapRecordsAsync()).Victories);
    }

    [Fact]
    public async Task MapRecordsAreIndependentAndSaveRetriesAreIdempotent()
    {
        var repository = new SqliteGameRepository(Database);
        var first = new RunResult("one", DateTimeOffset.UtcNow, 15, 200, 50, 500, "Victory", "outpost-07");
        var second = new RunResult("two", DateTimeOffset.UtcNow, 7, 90, 25, 200, "Retreated", "cryo-crossing");
        await repository.SaveRunAsync(first);
        await repository.SaveRunAsync(first);
        await repository.SaveRunAsync(second);
        var records = await repository.GetMapRecordsAsync();
        Assert.Equal(2, records.Count);
        Assert.Equal(new MapRecord("outpost-07", 15, 1, 1, 200), records.Single(record => record.MapId == "outpost-07"));
        Assert.Equal(new MapRecord("cryo-crossing", 7, 1, 0, 90), records.Single(record => record.MapId == "cryo-crossing"));
        Assert.Equal(75, (await repository.GetProfileAsync()).Credits);
    }

    [Fact]
    public async Task NewerDatabaseVersionIsNotSilentlyDowngraded()
    {
        Directory.CreateDirectory(directory);
        SQLitePCL.Batteries_V2.Init();
        using (var connection = new SqliteConnection($"Data Source={Database};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = 5";
            command.ExecuteNonQuery();
        }
        var repository = new SqliteGameRepository(Database);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetProfileAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}
