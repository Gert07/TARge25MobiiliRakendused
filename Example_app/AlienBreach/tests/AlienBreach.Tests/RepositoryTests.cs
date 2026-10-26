using AlienBreach.Core.Models;
using AlienBreach.Core.Persistence;

namespace AlienBreach.Tests;

public sealed class RepositoryTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "alien-breach-tests-" + Guid.NewGuid().ToString("N"));
    private SqliteGameRepository Repository() => new(Path.Combine(directory, "game.db"));
    private static RunResult Run(int credits = 100) => new(Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow, 3, 12, credits, 90, "Extracted");

    [Fact]
    public async Task ProfileAndHistoryPersistAcrossRepositoryInstances()
    {
        var result = Run();
        await Repository().SaveRunAsync(result);
        var reopened = Repository();
        var profile = await reopened.GetProfileAsync();
        Assert.Equal(100, profile.Credits);
        Assert.Equal(12, profile.TotalKills);
        Assert.Equal(3, profile.BestWave);
        Assert.Equal(1, profile.Runs);
        Assert.Equal(result, Assert.Single(await reopened.GetRecentRunsAsync()));
    }

    [Fact]
    public async Task RetryingSameRunCannotDuplicateCredits()
    {
        var repository = Repository();
        var result = Run();
        await Task.WhenAll(repository.SaveRunAsync(result), repository.SaveRunAsync(result));
        Assert.Equal(100, (await repository.GetProfileAsync()).Credits);
        Assert.Single(await repository.GetRecentRunsAsync());
    }

    [Fact]
    public async Task InsufficientFundsLeaveProfileUnchanged()
    {
        var repository = Repository();
        var purchase = await repository.PurchaseUpgradeAsync(UpgradeType.Firepower);
        Assert.False(purchase.Success);
        Assert.Equal(PlayerProfile.New, purchase.Profile);
    }

    [Fact]
    public async Task PurchaseDeductsCreditsAndPersistsLevel()
    {
        var repository = Repository();
        await repository.SaveRunAsync(Run());
        var purchase = await repository.PurchaseUpgradeAsync(UpgradeType.Fortification);
        Assert.True(purchase.Success);
        Assert.Equal(20, purchase.Profile.Credits);
        Assert.Equal(1, purchase.Profile.FortificationLevel);
        Assert.Equal(purchase.Profile, await Repository().GetProfileAsync());
    }

    [Fact]
    public async Task ConcurrentPurchasesCannotOverspend()
    {
        var repository = Repository();
        await repository.SaveRunAsync(Run());
        var purchases = await Task.WhenAll(repository.PurchaseUpgradeAsync(UpgradeType.Firepower),
            repository.PurchaseUpgradeAsync(UpgradeType.Fortification));
        Assert.Single(purchases, purchase => purchase.Success);
        Assert.Equal(20, (await repository.GetProfileAsync()).Credits);
    }

    [Fact]
    public async Task UpgradesStopAtLevelFive()
    {
        var repository = Repository();
        await repository.SaveRunAsync(Run(5000));
        for (var i = 0; i < 5; i++) Assert.True((await repository.PurchaseUpgradeAsync(UpgradeType.Firepower)).Success);
        var result = await repository.PurchaseUpgradeAsync(UpgradeType.Firepower);
        Assert.False(result.Success);
        Assert.Equal(5, result.Profile.FirepowerLevel);
        Assert.Equal(3800, result.Profile.Credits);
    }

    [Fact]
    public async Task InvalidRunDoesNotChangeDatabase()
    {
        var repository = Repository();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.SaveRunAsync(Run(-100)));
        Assert.Equal(PlayerProfile.New, await repository.GetProfileAsync());
        Assert.Empty(await repository.GetRecentRunsAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}
