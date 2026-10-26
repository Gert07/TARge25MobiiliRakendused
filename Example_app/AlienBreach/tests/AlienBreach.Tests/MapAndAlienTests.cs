using System.Numerics;
using AlienBreach.Core.Entities;
using AlienBreach.Core.Game;
using AlienBreach.Core.Maps;
using AlienBreach.Core.Models;
using AlienBreach.Core.Turrets;

namespace AlienBreach.Tests;

public sealed class MapAndAlienTests
{
    [Fact]
    public void ThreeMapsHaveDistinctRoutesAndBuildableGround()
    {
        Assert.Equal(3, MapCatalog.All.Count);
        Assert.Equal(3, MapCatalog.All.Select(map => map.Id).Distinct().Count());
        Assert.Equal(3, MapCatalog.All.Select(map => map.Path.Length).Distinct().Count());
        foreach (var map in MapCatalog.All)
        {
            Assert.All(map.Path.Points, point =>
            {
                Assert.InRange(point.X, 0, BattleMap.Width);
                Assert.InRange(point.Y, 0, BattleMap.Height);
                Assert.False(map.CanBuildAt(point, 23));
            });
            Assert.True(Candidates().Count(point => map.CanBuildAt(point, 23)) >= 20);
        }
    }

    [Fact]
    public void PathAdvancesCorrectlyAroundACorner()
    {
        var path = new EnemyPath(new(0, 0), new(100, 0), new(100, 100));
        Assert.Equal(new Vector2(100, 50), path.PositionAt(150));
        Assert.Equal(Vector2.UnitY, path.DirectionAt(150));
        Assert.Equal(new Vector2(100, 100), path.PositionAt(1000));
        Assert.Equal(25, path.DistanceTo(new(75, 25)));
    }

    [Theory]
    [InlineData(800, 620)]
    [InlineData(1100, 300)]
    public void ViewportMapsTouchesBackToTheSameWorldPosition(float width, float height)
    {
        var view = MapViewport.Fit(width, height);
        var point = new Vector2(650, 400);
        var screen = point * view.Scale + new Vector2(view.X, view.Y);
        Assert.True(Vector2.Distance(point, view.ToWorld(screen)!.Value) < .001f);
        Assert.Null(view.ToWorld(new(-10, -10)));
    }

    [Fact]
    public void TitanStartsAfterWaveFiveAndIsLargerAndStronger()
    {
        for (var wave = 1; wave <= 5; wave++)
            Assert.DoesNotContain(EnemyFactory.CreateWave(wave, Vector2.Zero), alien => alien is RedTitan);
        for (var wave = 6; wave <= 15; wave++)
        {
            var aliens = EnemyFactory.CreateWave(wave, Vector2.Zero);
            var titan = Assert.IsType<RedTitan>(aliens[0]);
            Assert.All(aliens.Where(alien => alien is not RedTitan), alien =>
            {
                Assert.True(titan.Radius > alien.Radius);
                Assert.True(titan.MaxHealth > alien.MaxHealth);
                Assert.True(titan.BreachDamage > alien.BreachDamage);
            });
        }
    }

    [Fact]
    public void CryoSlowsAliensButTitanResistsAndEffectsExpire()
    {
        var path = new EnemyPath(new(0, 100), new(1100, 100));
        var alien = new Stalker(new(0, 100), 6);
        var titan = new RedTitan(new(0, 100), 6);
        alien.ApplySlow(.45f, 2.5f);
        titan.ApplySlow(.45f, 2.5f);
        alien.Advance(path, 1);
        titan.Advance(path, 1);
        Assert.Equal(alien.Speed * .45f, alien.PathDistance, .01f);
        Assert.Equal(titan.Speed * .7f, titan.PathDistance, .01f);
        alien.Advance(path, 2);
        Assert.False(alien.IsSlowed);
        var before = alien.PathDistance;
        alien.Advance(path, 1);
        Assert.Equal(alien.Speed, alien.PathDistance - before, .01f);
    }

    [Fact]
    public void TargetingPrioritizesFrontAlienOrStrongestAlien()
    {
        var path = new EnemyPath(new(0, 100), new(1100, 100));
        var front = new Stalker(new(0, 100), 6);
        front.Advance(path, 2);
        var titan = new RedTitan(new(0, 100), 6);
        titan.Advance(path, .2f);
        Alien[] enemies = [front, titan];
        Assert.Same(front, new FirstTargeting().Select(enemies, new(100, 120), 200));
        Assert.Same(titan, new StrongestTargeting().Select(enemies, new(100, 120), 200));
        Assert.Null(new FirstTargeting().Select(enemies, new(1000, 600), 100));
    }

    [Fact]
    public void CatalogCreatesFiveDistinctTurretClasses()
    {
        Assert.Equal(5, TurretCatalog.All.Count);
        Assert.Equal(5, TurretCatalog.All.Select(definition =>
            TurretFactory.Create(definition.Type, new(100, 220), 0).GetType()).Distinct().Count());
    }

    [Theory]
    [InlineData("outpost-07")]
    [InlineData("reactor-ring")]
    [InlineData("cryo-crossing")]
    public void ARepresentativeDefenseCanReachTheEscalationWaves(string mapId)
    {
        var map = MapCatalog.Get(mapId);
        var game = new GameEngine(PlayerProfile.New, map);
        for (var wave = 1; wave <= 10 && game.State != GameState.Finished; wave++)
        {
            // Greedy coverage placement with a mix of all five turret roles.
            for (var purchase = 0; purchase < 30; purchase++)
            {
                var definition = TurretCatalog.All[game.Turrets.Count % 5];
                if (game.Cash < definition.Cost) definition = TurretCatalog.Get(TurretType.Pulse);
                if (game.Cash < definition.Cost) break;
                var candidates = Candidates().Where(game.CanPlace).ToArray();
                if (candidates.Length == 0) break;
                var position = candidates.MaxBy(point => Coverage(map.Path, point, definition.Range));
                Assert.True(game.PlaceTurret(definition.Type, position).Success);
            }
            foreach (var turret in game.Turrets)
                if (turret.Level < 3 && game.Cash >= turret.UpgradeCost) game.UpgradeTurret(turret);
            game.StartNextWave();
            for (var step = 0; step < 1600 && game.State == GameState.Running; step++) game.Update(.25f);
            Assert.NotEqual(GameState.Running, game.State);
        }
        Assert.Equal(GameState.Building, game.State);
        Assert.Null(game.Result);
        Assert.Equal(10, game.CompletedWaves);
        Assert.True(game.StartNextWave());
        Assert.Equal(11, game.Waves.Wave);
    }

    private static IEnumerable<Vector2> Candidates()
    {
        for (var x = 55; x < 1070; x += 65)
            for (var y = 55; y < 590; y += 65) yield return new(x, y);
    }
    private static float Coverage(EnemyPath path, Vector2 position, float range)
    {
        var result = 0f;
        for (var distance = 0f; distance < path.Length; distance += 25)
            if (Vector2.Distance(position, path.PositionAt(distance)) < range) result++;
        return result;
    }
}
