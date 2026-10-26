namespace AlienBreach.Core.Models;

public enum UpgradeType { Firepower, Fortification }

public sealed record PlayerProfile(int Credits, int TotalKills, int BestWave,
    int Runs, int FirepowerLevel, int FortificationLevel)
{
    public static PlayerProfile New => new(0, 0, 0, 0, 0, 0);
    public const int MaxUpgradeLevel = 5;
    public int Level(UpgradeType type) => type == UpgradeType.Firepower ? FirepowerLevel : FortificationLevel;
    public int UpgradeCost(UpgradeType type) => 80 * (Level(type) + 1);
}

public sealed record RunResult(string Id, DateTimeOffset FinishedAt, int Wave, int Kills,
    int Credits, double Seconds, string Outcome, string MapId = "legacy-survival");

public sealed record MapRecord(string MapId, int BestWave, int Runs, int Victories, int BestKills);
public sealed record PurchaseResult(bool Success, string Message, PlayerProfile Profile);
