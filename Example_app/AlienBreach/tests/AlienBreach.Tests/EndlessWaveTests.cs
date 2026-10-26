using System.Numerics;
using AlienBreach.Core.Entities;
using AlienBreach.Core.Game;
using AlienBreach.Core.Maps;
using AlienBreach.Core.Models;

namespace AlienBreach.Tests;

public sealed class EndlessWaveTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(10)]
    public void AllAlienSpeciesHaveFiftyPercentMoreHealthBeforeEscalation(int wave)
    {
        Assert.Equal((28 + wave * 5) * 1.5f, new Stalker(Vector2.Zero, wave).MaxHealth, .01f);
        Assert.Equal((42 + wave * 7) * 1.5f, new Spitter(Vector2.Zero, wave).MaxHealth, .01f);
        Assert.Equal((88 + wave * 12) * 1.5f, new Brute(Vector2.Zero, wave).MaxHealth, .01f);
        Assert.Equal((360 + wave * 45) * 1.5f, new RedTitan(Vector2.Zero, wave).MaxHealth, .01f);
    }

    [Theory]
    [InlineData(10, 1.5f)]
    [InlineData(11, 1.8f)]
    [InlineData(12, 2.16f)]
    [InlineData(13, 2.592f)]
    [InlineData(20, 9.287604f)]
    public void TwentyPercentHealthEscalationCompoundsAfterWaveTen(int wave, float expected)
    {
        Assert.Equal(expected, EnemyDifficulty.HealthMultiplier(wave), .001f);
        Assert.Equal((28 + wave * 5) * expected, new Stalker(Vector2.Zero, wave).MaxHealth, .05f);
        Assert.Equal((360 + wave * 45) * expected, new RedTitan(Vector2.Zero, wave).MaxHealth, .1f);
    }

    [Theory]
    [InlineData("outpost-07")]
    [InlineData("reactor-ring")]
    [InlineData("cryo-crossing")]
    public void EveryMapCanContinueBeyondTheOldFifteenWaveLimit(string mapId)
    {
        var game = new GameEngine(PlayerProfile.New, MapCatalog.Get(mapId));
        var finishedEvents = 0;
        game.RunFinished += _ => finishedEvents++;
        for (var wave = 1; wave <= 25; wave++)
        {
            Assert.True(game.StartNextWave());
            for (var tick = 0; tick < 1600 && game.State == GameState.Running; tick++)
            {
                // Eliminate spawned enemies to isolate progression from defense balance.
                foreach (var alien in game.Aliens) alien.TakeDamage(float.MaxValue);
                game.Update(.25f);
            }
            Assert.Equal(GameState.Building, game.State);
            Assert.Equal(wave, game.CompletedWaves);
            Assert.Equal(20, game.Lives);
            Assert.Null(game.Result);
        }
        Assert.Equal(0, finishedEvents);
        Assert.True(game.StartNextWave());
        Assert.Equal(26, game.Waves.Wave);
        game.Retreat();
        Assert.Equal(1, finishedEvents);
        Assert.Equal(26, game.Result!.Wave);
    }

    [Fact]
    public void VeryLateWavesHaveFiniteHealthAndPositiveSpeeds()
    {
        Alien[] aliens = [new Stalker(Vector2.Zero, 1000), new Spitter(Vector2.Zero, 1000),
            new Brute(Vector2.Zero, 1000), new RedTitan(Vector2.Zero, 1000)];
        Assert.All(aliens, alien =>
        {
            Assert.True(float.IsFinite(alien.MaxHealth));
            Assert.True(alien.MaxHealth > 0);
            Assert.True(alien.Speed > 0);
        });
    }
}
