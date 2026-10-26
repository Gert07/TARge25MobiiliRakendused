using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Globalization;
using Example_app.Models;
using Example_app.Services;
using Microsoft.Data.Sqlite;

static void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException(description);
    Console.WriteLine($"PASS: {description}");
}

var expected = new AppSettings { LanguageCode = "en", Theme = "Dark", GameMode = 1, HasSeenWelcome = true };

if (args is ["--verify-restart", var restartPath])
{
    var reopened = new AppDatabase(restartPath);
    // Uus protsess jäljendab rakenduse järgmist käivitamist.
    reopened.ImportLegacyData(new AppSettings(), new GameStats { XWins = 99 });
    Check(reopened.LoadSettings() == expected, "Language, theme, opponent and welcome state survive process restart");
    Check(reopened.LoadStats() == new GameStats { XWins = 8, OWins = 5, Draws = 3 }, "Statistics survive restart; migration does not overwrite SQLite data");
    return;
}

string folder = Path.Combine(Path.GetTempPath(), $"ExampleAppChecks-{Guid.NewGuid():N}");
Directory.CreateDirectory(folder);
try
{
    string path = Path.Combine(folder, "settings.db3");
    var database = new AppDatabase(path);
    Check(database.LoadSettings() == new AppSettings(), "New install has Estonian, light theme and two players");
    database.ImportLegacyData(new AppSettings(), new GameStats { XWins = 7, OWins = 4, Draws = 2 });
    Check(database.LoadStats().XWins == 7, "Existing Preferences statistics are imported");

    database.SaveSettings(expected);
    Check(database.LoadSettings() == expected, "All settings are committed immediately");
    database.RecordResult("X");
    database.RecordResult("O");
    database.RecordResult("");

    var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true };
    start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
    start.ArgumentList.Add("--verify-restart");
    start.ArgumentList.Add(path);
    using var child = Process.Start(start) ?? throw new InvalidOperationException("Could not start restart check.");
    child.WaitForExit();
    Check(child.ExitCode == 0, "Independent process can restore the persisted state");

    database.ResetStats();
    Check(database.LoadStats() == new GameStats(), "Statistics reset is persisted");
    Check(database.LoadSettings() == expected, "Clearing statistics keeps settings");

    bool rejected = false;
    try { database.SaveSettings(expected with { LanguageCode = "unknown" }); }
    catch (ArgumentException) { rejected = true; }
    Check(rejected && database.LoadSettings() == expected, "Invalid settings do not replace saved values");

    var resources = new ResourceManager("Example_app.Resources.Localization.AppResources", typeof(LanguageService).Assembly);
    var english = resources.GetResourceSet(CultureInfo.GetCultureInfo("en"), true, true)!;
    var estonian = resources.GetResourceSet(CultureInfo.GetCultureInfo("et"), true, false)!;
    int count = 0;
    foreach (System.Collections.DictionaryEntry item in english)
    {
        if (estonian.GetString((string)item.Key) is not { Length: > 0 })
            throw new InvalidOperationException($"Missing Estonian translation: {item.Key}");
        count++;
    }
    Check(count == estonian.Cast<System.Collections.DictionaryEntry>().Count(), $"All {count} resource keys have both translations");
    int notifications = 0;
    LanguageService.Instance.PropertyChanged += (_, _) => notifications++;
    LanguageService.Instance.ApplyLanguage("et");
    Check(LanguageService.Instance["Settings"] == "Sätted", "Estonian resources are loaded");
    LanguageService.Instance.ApplyLanguage("en");
    Check(LanguageService.Instance["Settings"] == "Settings" && notifications == 2, "Language changes notify live bindings");
    Check(LanguageService.Instance.Format("PlayerTurn", "O") == "Player O's turn", "Dynamic game status uses the selected language");
}
finally
{
    SqliteConnection.ClearAllPools();
    // Kustutame ainult selle kontrolli loodud ajutised failid.
    foreach (string file in Directory.EnumerateFiles(folder)) File.Delete(file);
    Directory.Delete(folder);
}
