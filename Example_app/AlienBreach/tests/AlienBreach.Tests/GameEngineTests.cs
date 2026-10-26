using System.Numerics;
using AlienBreach.Core.Entities;
using AlienBreach.Core.Game;
using AlienBreach.Core.Maps;
using AlienBreach.Core.Models;
using AlienBreach.Core.Turrets;

namespace AlienBreach.Tests;

public sealed class GameEngineTests
{
    private static GameEngine Game() => new(PlayerProfile.New, MapCatalog.All[0]);

    [Fact]
    public void MissionWaitsForPlayerToStartFirstWave()
    {
        var game = Game();
        game.Update(10);
        Assert.Equal(GameState.Building, game.State);
        Assert.Equal(350, game.Cash);
        Assert.Empty(game.Aliens);
        Assert.Equal(0, game.Waves.Wave);
    }

    [Fact]
    public void InvalidPlacementsDoNotSpendSupplies()
    {
        var game = Game();
        Assert.False(game.PlaceTurret(TurretType.Pulse, new(200, 145)).Success);
        Assert.False(game.PlaceTurret(TurretType.Pulse, new(-1, 220)).Success);
        Assert.False(game.PlaceTurret(TurretType.Pulse, new(float.NaN, 220)).Success);
        Assert.Equal(350, game.Cash);
        Assert.Empty(game.Turrets);
    }

    [Fact]
    public void ValidPlacementCostsSuppliesAndPreventsOverlap()
    {
        var game = Game();
        var result = game.PlaceTurret(TurretType.Pulse, new(100, 220));
        Assert.True(result.Success);
        Assert.Equal(260, game.Cash);
        Assert.False(game.PlaceTurret(TurretType.Pulse, new(110, 220)).Success);
        Assert.Single(game.Turrets);
        Assert.Equal(260, game.Cash);
    }

    [Fact]
    public void CannotBuildWithoutSupplies()
    {
        var game = Game();
        Assert.True(game.PlaceTurret(TurretType.Railgun, new(100, 220)).Success);
        Assert.False(game.PlaceTurret(TurretType.Railgun, new(400, 300)).Success);
        Assert.Equal(110, game.Cash);
    }

    [Fact]
    public void UpgradeAndSaleUseInvestedCostAndCannotRepeatSale()
    {
        var game = Game();
        var turret = game.PlaceTurret(TurretType.Pulse, new(100, 220)).Turret!;
        var damage = turret.Damage;
        var range = turret.Range;
        Assert.True(game.UpgradeTurret(turret).Success);
        Assert.Equal(2, turret.Level);
        Assert.True(turret.Damage > damage);
        Assert.True(turret.Range > range);
        Assert.Equal(197, game.Cash);
        var expected = game.Cash + turret.SellValue;
        Assert.True(game.SellTurret(turret).Success);
        Assert.Equal(expected, game.Cash);
        Assert.False(game.SellTurret(turret).Success);
        Assert.Equal(expected, game.Cash);
        Assert.False(game.UpgradeTurret(turret).Success);
    }

    [Fact]
    public void TurretUpgradesStopAtLevelThree()
    {
        var game = Game();
        var turret = game.PlaceTurret(TurretType.Pulse, new(100, 220)).Turret!;
        Assert.True(game.UpgradeTurret(turret).Success);
        Assert.True(game.UpgradeTurret(turret).Success);
        var balance = game.Cash;
        Assert.False(game.UpgradeTurret(turret).Success);
        Assert.Equal(3, turret.Level);
        Assert.Equal(balance, game.Cash);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PauseFreezesAndRestoresThePreviousPhase(bool startWave)
    {
        var game = Game();
        if (startWave) game.StartNextWave();
        var previous = game.State;
        game.TogglePause();
        game.Update(.25f);
        Assert.Equal(0, game.ElapsedSeconds);
        Assert.Empty(game.Aliens);
        Assert.False(game.PlaceTurret(TurretType.Pulse, new(100, 220)).Success);
        Assert.False(game.StartNextWave());
        game.TogglePause();
        Assert.Equal(previous, game.State);
    }

    [Fact]
    public void CannotStartOverlappingWaves()
    {
        var game = Game();
        Assert.True(game.StartNextWave());
        Assert.False(game.StartNextWave());
        Assert.Equal(1, game.Waves.Wave);
    }

    [Fact]
    public void PulseTurretActuallyKillsRouteEnemies()
    {
        var game = Game();
        game.PlaceTurret(TurretType.Pulse, new(100, 220));
        game.StartNextWave();
        for (var i = 0; i < 80; i++) game.Update(.1f);
        Assert.True(game.Kills > 0);
        Assert.True(game.Credits > 0);
        Assert.True(game.Cash > 260);
    }

    [Fact]
    public void SplashHitsNearbyAliensAndRewardsEachOnlyOnce()
    {
        var game = Game();
        game.StartNextWave();
        for (var i = 0; i < 10; i++) game.Update(.1f);
        Assert.Equal(2, game.Aliens.Count);
        game.ResolveHit(game.Aliens[0], 60, ShotKind.Plasma);
        game.Update(.01f);
        Assert.Equal(2, game.Kills);
        var cash = game.Cash;
        game.Update(.01f);
        Assert.Equal(2, game.Kills);
        Assert.Equal(cash, game.Cash);
    }

    [Fact]
    public void CryoShotAppliesDamageAndSlow()
    {
        var game = Game();
        game.StartNextWave();
        game.Update(.1f);
        var target = Assert.Single(game.Aliens);
        game.ResolveHit(target, 8, ShotKind.Cryo);
        Assert.Equal(target.MaxHealth - 8, target.Health);
        Assert.True(target.IsSlowed);
    }

    [Fact]
    public void RailBeamPiercesAtMostThreeAliens()
    {
        var map = new BattleMap("test", "Test", "", "", MapTheme.Outpost, new EnemyPath(new(0, 100), new(1100, 100)));
        var game = new GameEngine(PlayerProfile.New, map);
        game.StartNextWave();
        for (var i = 0; i < 30; i++) game.Update(.1f);
        Assert.Equal(4, game.Aliens.Count);
        game.FireRail(new RailgunTurret(new(0, 100)), game.Aliens[0]);
        Assert.Equal(3, game.Aliens.Count(alien => !alien.IsAlive));
    }

    [Fact]
    public void EscapesDamageBaseOnceAndDoNotAwardKillCash()
    {
        var map = new BattleMap("test", "Test", "", "", MapTheme.Outpost, new EnemyPath(new(0, 100), new(15, 100)));
        var game = new GameEngine(PlayerProfile.New, map);
        game.StartNextWave();
        for (var i = 0; i < 200 && game.State == GameState.Running; i++) game.Update(.1f);
        Assert.Equal(12, game.Lives);
        Assert.Equal(0, game.Kills);
        Assert.Equal(418, game.Cash); // starting supplies plus one clear bonus, no kills
        Assert.Equal(1, game.CompletedWaves);
        Assert.Equal(GameState.Building, game.State);
        game.Update(1);
        Assert.Equal(12, game.Lives);
    }

    [Fact]
    public void ClearingAWaveAllowsAnotherWaveAndRetreatProducesOneResult()
    {
        var map = new BattleMap("test", "Test", "", "", MapTheme.Outpost, new EnemyPath(new(0, 100), new(15, 100)));
        var game = new GameEngine(PlayerProfile.New, map);
        var events = 0;
        game.RunFinished += _ => events++;
        game.StartNextWave();
        for (var i = 0; i < 100; i++) game.Update(.1f);
        Assert.Equal(GameState.Building, game.State);
        Assert.Equal(0, events);
        Assert.Null(game.Result);
        Assert.True(game.StartNextWave());
        game.Retreat();
        game.Retreat();
        Assert.Equal(1, events);
        Assert.Equal("Retreated", game.Result!.Outcome);
        Assert.Equal("test", game.Result.MapId);
        Assert.False(game.StartNextWave());
    }

    [Fact]
    public void ZeroIntegrityEndsMission()
    {
        var map = new BattleMap("test", "Test", "", "", MapTheme.Outpost, new EnemyPath(new(0, 100), new(15, 100)));
        var game = new GameEngine(PlayerProfile.New, map);
        for (var i = 0; i < 600 && game.State != GameState.Finished; i++)
        {
            if (game.State == GameState.Building) game.StartNextWave();
            game.Update(.1f);
        }
        Assert.Equal(0, game.Lives);
        Assert.Equal("Overrun", game.Result!.Outcome);
    }

    [Fact]
    public void ResearchBoostsAllTurretsAndBaseWithoutSpendingSavedCredits()
    {
        var profile = PlayerProfile.New with { Credits = 123, FirepowerLevel = 2, FortificationLevel = 3 };
        var game = new GameEngine(profile, MapCatalog.All[0]);
        var turret = game.PlaceTurret(TurretType.Pulse, new(100, 220)).Turret!;
        Assert.Equal(26, game.MaxLives);
        Assert.Equal(18 * 1.16f, turret.Damage, .001f);
        Assert.Equal(123, profile.Credits);
    }
}
