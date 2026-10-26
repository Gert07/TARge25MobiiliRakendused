namespace Example_app.Models;

// Üks seadete rida andmebaasis (Id = 1).
public sealed record AppSettings
{
    public string LanguageCode { get; init; } = "et";
    public string Theme { get; init; } = "Light";
    public int GameMode { get; init; }
    public bool HasSeenWelcome { get; init; }
}
