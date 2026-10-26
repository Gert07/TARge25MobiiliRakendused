using AlienBreach.Core.Models;

namespace AlienBreach.Core.Persistence;

public interface IGameRepository
{
    Task<GameSettings> GetSettingsAsync();
    Task SaveSettingsAsync(GameSettings settings);
    Task<PlayerProfile> GetProfileAsync();
    Task<IReadOnlyList<RunResult>> GetRecentRunsAsync(int limit = 12);
    Task<IReadOnlyList<MapRecord>> GetMapRecordsAsync();
    Task SaveRunAsync(RunResult result);
    Task<PurchaseResult> PurchaseUpgradeAsync(UpgradeType type);
}
