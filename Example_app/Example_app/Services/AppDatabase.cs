using Example_app.Models;
using Microsoft.Data.Sqlite;

namespace Example_app.Services;

public sealed class AppDatabase
{
    private readonly string _connectionString;

    public AppDatabase(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS AppSettings (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                LanguageCode TEXT NOT NULL,
                Theme TEXT NOT NULL,
                GameMode INTEGER NOT NULL CHECK (GameMode IN (0, 1)),
                HasSeenWelcome INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS GameStats (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                XWins INTEGER NOT NULL DEFAULT 0,
                OWins INTEGER NOT NULL DEFAULT 0,
                Draws INTEGER NOT NULL DEFAULT 0
            );
            """;
        command.ExecuteNonQuery();
    }

    // INSERT OR IGNORE impordib Preferences-andmed ainult esimesel käivitamisel.
    public void ImportLegacyData(AppSettings settings, GameStats stats)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT OR IGNORE INTO AppSettings VALUES (1, $language, $theme, $mode, $welcome);
            INSERT OR IGNORE INTO GameStats VALUES (1, $x, $o, $draws);
            """;
        AddSettingsParameters(command, settings);
        command.Parameters.AddWithValue("$x", stats.XWins);
        command.Parameters.AddWithValue("$o", stats.OWins);
        command.Parameters.AddWithValue("$draws", stats.Draws);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public AppSettings LoadSettings()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT LanguageCode, Theme, GameMode, HasSeenWelcome FROM AppSettings WHERE Id = 1";
        using var reader = command.ExecuteReader();
        return reader.Read() ? new AppSettings
        {
            LanguageCode = reader.GetString(0),
            Theme = reader.GetString(1),
            GameMode = reader.GetInt32(2),
            HasSeenWelcome = reader.GetBoolean(3)
        } : new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        if (settings.LanguageCode is not ("et" or "en") ||
            settings.Theme is not ("Light" or "Dark") || settings.GameMode is < 0 or > 1)
            throw new ArgumentException("Unsupported settings.", nameof(settings));

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO AppSettings VALUES (1, $language, $theme, $mode, $welcome)
            ON CONFLICT(Id) DO UPDATE SET LanguageCode = excluded.LanguageCode,
                Theme = excluded.Theme, GameMode = excluded.GameMode,
                HasSeenWelcome = excluded.HasSeenWelcome;
            """;
        AddSettingsParameters(command, settings);
        command.ExecuteNonQuery();
    }

    public GameStats LoadStats()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT XWins, OWins, Draws FROM GameStats WHERE Id = 1";
        using var reader = command.ExecuteReader();
        return reader.Read() ? new GameStats
        {
            XWins = reader.GetInt32(0), OWins = reader.GetInt32(1), Draws = reader.GetInt32(2)
        } : new GameStats();
    }

    public void RecordResult(string winner)
    {
        string column = winner switch
        {
            "X" => "XWins",
            "O" => "OWins",
            "" => "Draws",
            _ => throw new ArgumentException("Unknown winner.", nameof(winner))
        };
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        // Veerunimi pärineb ainult ülalolevast lubatud väärtuste loendist.
        command.CommandText = $"""
            INSERT INTO GameStats (Id, {column}) VALUES (1, 1)
            ON CONFLICT(Id) DO UPDATE SET {column} = {column} + 1;
            """;
        command.ExecuteNonQuery();
    }

    public void ResetStats()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE GameStats SET XWins = 0, OWins = 0, Draws = 0 WHERE Id = 1";
        command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static void AddSettingsParameters(SqliteCommand command, AppSettings settings)
    {
        command.Parameters.AddWithValue("$language", settings.LanguageCode);
        command.Parameters.AddWithValue("$theme", settings.Theme);
        command.Parameters.AddWithValue("$mode", settings.GameMode);
        command.Parameters.AddWithValue("$welcome", settings.HasSeenWelcome);
    }
}
