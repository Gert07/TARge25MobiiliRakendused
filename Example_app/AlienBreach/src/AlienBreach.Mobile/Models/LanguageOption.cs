namespace AlienBreach.Models;

public sealed record LanguageOption(string Code, string Name)
{
    public static IReadOnlyList<LanguageOption> All { get; } =
        Array.AsReadOnly(new[] { new LanguageOption("et", "Eesti"), new LanguageOption("en", "English") });
}
