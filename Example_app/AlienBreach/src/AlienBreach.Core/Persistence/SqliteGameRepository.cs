using System.Globalization;
using AlienBreach.Core.Models;
using Microsoft.Data.Sqlite;

namespace AlienBreach.Core.Persistence;

public sealed class SqliteGameRepository : IGameRepository
{
    private static readonly Lazy<bool> NativeLibrary = new(() => { SQLitePCL.Batteries_V2.Init(); return true; });
    private readonly object gate = new();
    private readonly string connectionString;
    private bool initialized;

    public SqliteGameRepository(string databasePath)
    {
        _ = NativeLibrary.Value;
        var fullPath = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false
        }.ToString();
    }

    // Work runs off the UI thread; each operation owns its connection.
    private Task<T> ExecuteAsync<T>(Func<SqliteConnection, T> action) => Task.Run(() =>
    {
        lock (gate)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            if (!initialized)
            {
                using var version = connection.CreateCommand();
                version.CommandText = "PRAGMA user_version";
                if (Convert.ToInt32(version.ExecuteScalar()) > 4)
                    throw new InvalidOperationException("This database belongs to a newer version of Alien Breach.");
                using var migration = connection.BeginTransaction();
                using var schema = connection.CreateCommand();
                schema.Transaction = migration;
                schema.CommandText = """
                    CREATE TABLE IF NOT EXISTS Settings (
                        Id INTEGER PRIMARY KEY CHECK (Id = 1),
                        Language TEXT NOT NULL DEFAULT 'et' CHECK (Language IN ('et', 'en')),
                        GameSpeed INTEGER NOT NULL DEFAULT 1 CHECK (GameSpeed IN (1, 2)),
                        SoundEnabled INTEGER NOT NULL DEFAULT 1 CHECK (SoundEnabled IN (0, 1))
                    );
                    INSERT OR IGNORE INTO Settings (Id) VALUES (1);
                    CREATE TABLE IF NOT EXISTS PlayerProfile (
                        Id INTEGER PRIMARY KEY CHECK (Id = 1),
                        Credits INTEGER NOT NULL DEFAULT 0 CHECK (Credits >= 0),
                        TotalKills INTEGER NOT NULL DEFAULT 0,
                        BestWave INTEGER NOT NULL DEFAULT 0,
                        Runs INTEGER NOT NULL DEFAULT 0,
                        RifleLevel INTEGER NOT NULL DEFAULT 0 CHECK (RifleLevel BETWEEN 0 AND 5),
                        ArmorLevel INTEGER NOT NULL DEFAULT 0 CHECK (ArmorLevel BETWEEN 0 AND 5)
                    );
                    INSERT OR IGNORE INTO PlayerProfile (Id) VALUES (1);
                    CREATE TABLE IF NOT EXISTS Runs (
                        Id TEXT PRIMARY KEY,
                        FinishedAt TEXT NOT NULL,
                        Wave INTEGER NOT NULL,
                        Kills INTEGER NOT NULL,
                        Credits INTEGER NOT NULL,
                        Seconds REAL NOT NULL,
                        Outcome TEXT NOT NULL,
                        MapId TEXT NOT NULL DEFAULT 'legacy-survival'
                    );
                    CREATE INDEX IF NOT EXISTS IX_Runs_FinishedAt ON Runs (FinishedAt DESC);
                    CREATE TABLE IF NOT EXISTS MapRecords (
                        MapId TEXT PRIMARY KEY,
                        BestWave INTEGER NOT NULL,
                        Runs INTEGER NOT NULL,
                        Victories INTEGER NOT NULL,
                        BestKills INTEGER NOT NULL
                    );
                    """;
                schema.ExecuteNonQuery();
                using var columns = Command(connection, migration, "PRAGMA table_info(Runs)");
                bool hasMapId = false;
                using (var reader = columns.ExecuteReader())
                    while (reader.Read()) if (reader.GetString(1) == "MapId") hasMapId = true;
                if (!hasMapId)
                {
                    using var alter = Command(connection, migration,
                        "ALTER TABLE Runs ADD COLUMN MapId TEXT NOT NULL DEFAULT 'legacy-survival'");
                    alter.ExecuteNonQuery();
                }
                using var settingsColumns = Command(connection, migration, "PRAGMA table_info(Settings)");
                bool hasSoundEnabled = false;
                using (var reader = settingsColumns.ExecuteReader())
                    while (reader.Read()) if (reader.GetString(1) == "SoundEnabled") hasSoundEnabled = true;
                if (!hasSoundEnabled)
                {
                    using var alter = Command(connection, migration,
                        "ALTER TABLE Settings ADD COLUMN SoundEnabled INTEGER NOT NULL DEFAULT 1 CHECK (SoundEnabled IN (0, 1))");
                    alter.ExecuteNonQuery();
                }
                using var stamp = Command(connection, migration, "PRAGMA user_version = 4");
                stamp.ExecuteNonQuery();
                migration.Commit();
                initialized = true;
            }
            return action(connection);
        }
    });

    public Task<PlayerProfile> GetProfileAsync() => ExecuteAsync(connection => ReadProfile(connection));

    public Task<GameSettings> GetSettingsAsync() => ExecuteAsync(connection =>
    {
        using var command = Command(connection, null, "SELECT Language, GameSpeed, SoundEnabled FROM Settings WHERE Id = 1");
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException("The settings are missing.");
        var settings = new GameSettings(reader.GetString(0), reader.GetInt32(1), reader.GetBoolean(2));
        settings.Validate();
        return settings;
    });

    public Task SaveSettingsAsync(GameSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        return ExecuteAsync(connection =>
        {
            using var command = Command(connection, null,
                "UPDATE Settings SET Language = $language, GameSpeed = $speed, SoundEnabled = $sound WHERE Id = 1",
                ("$language", settings.Language), ("$speed", settings.GameSpeed), ("$sound", settings.SoundEnabled ? 1 : 0));
            command.ExecuteNonQuery();
            return true;
        });
    }

    public Task<IReadOnlyList<RunResult>> GetRecentRunsAsync(int limit = 12) => ExecuteAsync<IReadOnlyList<RunResult>>(connection =>
    {
        using var command = Command(connection, null,
            "SELECT Id, FinishedAt, Wave, Kills, Credits, Seconds, Outcome, MapId FROM Runs ORDER BY FinishedAt DESC LIMIT $limit",
            ("$limit", Math.Clamp(limit, 1, 100)));
        using var reader = command.ExecuteReader();
        var runs = new List<RunResult>();
        while (reader.Read())
            runs.Add(new RunResult(reader.GetString(0), DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4), reader.GetDouble(5), reader.GetString(6), reader.GetString(7)));
        return runs;
    });

    public Task<IReadOnlyList<MapRecord>> GetMapRecordsAsync() => ExecuteAsync<IReadOnlyList<MapRecord>>(connection =>
    {
        using var command = Command(connection, null, "SELECT MapId, BestWave, Runs, Victories, BestKills FROM MapRecords");
        using var reader = command.ExecuteReader();
        var records = new List<MapRecord>();
        while (reader.Read()) records.Add(new MapRecord(reader.GetString(0), reader.GetInt32(1),
            reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4)));
        return records;
    });

    public Task SaveRunAsync(RunResult result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(result.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(result.MapId);
        if (result.Credits < 0 || result.Kills < 0 || result.Wave < 0 || result.Seconds < 0 || !double.IsFinite(result.Seconds))
            throw new ArgumentOutOfRangeException(nameof(result));
        return ExecuteAsync(connection =>
        {
            using var transaction = connection.BeginTransaction();
            using var insert = Command(connection, transaction, """
                INSERT OR IGNORE INTO Runs (Id, FinishedAt, Wave, Kills, Credits, Seconds, Outcome, MapId)
                VALUES ($id, $date, $wave, $kills, $credits, $seconds, $outcome, $map)
                """, ("$id", result.Id), ("$date", result.FinishedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
                ("$wave", result.Wave), ("$kills", result.Kills), ("$credits", result.Credits),
                ("$seconds", result.Seconds), ("$outcome", result.Outcome), ("$map", result.MapId));
            if (insert.ExecuteNonQuery() == 1)
            {
                using var update = Command(connection, transaction, """
                    UPDATE PlayerProfile SET Credits = Credits + $credits, TotalKills = TotalKills + $kills,
                        BestWave = MAX(BestWave, $wave), Runs = Runs + 1 WHERE Id = 1
                    """, ("$credits", result.Credits), ("$kills", result.Kills), ("$wave", result.Wave));
                update.ExecuteNonQuery();
                using var map = Command(connection, transaction, """
                    INSERT INTO MapRecords (MapId, BestWave, Runs, Victories, BestKills)
                    VALUES ($map, $wave, 1, $victory, $kills)
                    ON CONFLICT(MapId) DO UPDATE SET BestWave = MAX(BestWave, $wave),
                        Runs = Runs + 1, Victories = Victories + $victory, BestKills = MAX(BestKills, $kills)
                    """, ("$map", result.MapId), ("$wave", result.Wave), ("$kills", result.Kills),
                    ("$victory", result.Outcome == "Victory" ? 1 : 0));
                map.ExecuteNonQuery();
            }
            transaction.Commit();
            return true;
        });
    }

    public Task<PurchaseResult> PurchaseUpgradeAsync(UpgradeType type)
    {
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        return ExecuteAsync(connection =>
        {
            using var transaction = connection.BeginTransaction();
            var profile = ReadProfile(connection, transaction);
            if (profile.Level(type) >= PlayerProfile.MaxUpgradeLevel)
                return new PurchaseResult(false, "This upgrade is already maxed out.", profile);
            var cost = profile.UpgradeCost(type);
            if (profile.Credits < cost)
                return new PurchaseResult(false, $"You need {cost - profile.Credits} more credits.", profile);
            // Legacy column names retain the original player's purchased upgrades.
            var column = type == UpgradeType.Firepower ? "RifleLevel" : "ArmorLevel";
            using var command = Command(connection, transaction,
                $"UPDATE PlayerProfile SET Credits = Credits - $cost, {column} = {column} + 1 WHERE Id = 1", ("$cost", cost));
            command.ExecuteNonQuery();
            var updated = ReadProfile(connection, transaction);
            transaction.Commit();
            return new PurchaseResult(true, "Upgrade installed. Ready for your next deployment.", updated);
        });
    }

    private static PlayerProfile ReadProfile(SqliteConnection connection, SqliteTransaction? transaction = null)
    {
        using var command = Command(connection, transaction,
            "SELECT Credits, TotalKills, BestWave, Runs, RifleLevel, ArmorLevel FROM PlayerProfile WHERE Id = 1");
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException("The player profile is missing.");
        return new PlayerProfile(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2),
            reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5));
    }

    private static SqliteCommand Command(SqliteConnection connection, SqliteTransaction? transaction,
        string sql, params (string Name, object Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        return command;
    }
}
