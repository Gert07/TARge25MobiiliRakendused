using System.Numerics;
using AlienBreach.Core.Entities;
using AlienBreach.Core.Maps;
using AlienBreach.Core.Models;
using AlienBreach.Core.Turrets;

namespace AlienBreach.Core.Game;

public enum GameState { Building, Running, Paused, Finished }
public sealed record BuildResult(bool Success, string Message, Turret? Turret = null,
    string? MessageKey = null, int Amount = 0);

public sealed class GameEngine
{
    private readonly List<Alien> aliens = [];
    private readonly List<Turret> turrets = [];
    private readonly List<Projectile> projectiles = [];
    private readonly List<Impact> impacts = [];
    private readonly PlayerProfile profile;
    private readonly string runId = Guid.NewGuid().ToString("N");
    private GameState beforePause;

    public GameEngine(PlayerProfile profile, BattleMap map)
    {
        this.profile = profile;
        Map = map;
        Cash = map.StartingCash;
        Lives = MaxLives = 20 + profile.FortificationLevel * 2;
    }

    public BattleMap Map { get; }
    public WaveDirector Waves { get; } = new();
    public GameState State { get; private set; } = GameState.Building;
    public IReadOnlyList<Alien> Aliens => aliens.AsReadOnly();
    public IReadOnlyList<Turret> Turrets => turrets.AsReadOnly();
    public IReadOnlyList<Projectile> Projectiles => projectiles.AsReadOnly();
    public IReadOnlyList<Impact> Impacts => impacts.AsReadOnly();
    public int Cash { get; private set; }
    public int Lives { get; private set; }
    public int MaxLives { get; }
    public int CompletedWaves { get; private set; }
    public int Kills { get; private set; }
    public int Credits { get; private set; }
    public double ElapsedSeconds { get; private set; }
    public RunResult? Result { get; private set; }
    public event Action<RunResult>? RunFinished;
    public event Action<SoundCue>? SoundRequested;

    public bool CanPlace(Vector2 position) => Map.CanBuildAt(position, 23) &&
        turrets.All(turret => Vector2.Distance(turret.Position, position) >= turret.Radius + 23 + 8);

    public BuildResult PlaceTurret(TurretType type, Vector2 position)
    {
        if (State is GameState.Paused or GameState.Finished) return new(false, "Resume the mission to build.", MessageKey: "ResumeToBuild");
        var definition = TurretCatalog.Get(type);
        if (Cash < definition.Cost) return new(false, $"Need ${definition.Cost - Cash} more supplies.", MessageKey: "NeedSupplies", Amount: definition.Cost - Cash);
        if (!CanPlace(position)) return new(false, "Build off the route, inside the map, and clear of other turrets.", MessageKey: "InvalidPlacement");
        var turret = TurretFactory.Create(type, position, profile.FirepowerLevel);
        turrets.Add(turret);
        Cash -= definition.Cost;
        SoundRequested?.Invoke(SoundCue.Build);
        return new(true, $"{definition.Name} deployed.", turret);
    }

    public BuildResult UpgradeTurret(Turret turret)
    {
        if (State is GameState.Paused or GameState.Finished || !turrets.Contains(turret))
            return new(false, "Select an active turret while the mission is open.", MessageKey: "SelectActiveTurret");
        if (turret.Level >= 3) return new(false, "Turret is at maximum level.", MessageKey: "TurretMaxLevel");
        if (Cash < turret.UpgradeCost) return new(false, $"Need ${turret.UpgradeCost - Cash} more supplies.", MessageKey: "NeedSupplies", Amount: turret.UpgradeCost - Cash);
        Cash -= turret.UpgradeCost;
        turret.Upgrade();
        SoundRequested?.Invoke(SoundCue.Build);
        return new(true, $"{turret.Definition.Name} upgraded to level {turret.Level}.", turret);
    }

    public BuildResult SellTurret(Turret turret)
    {
        if (State is GameState.Paused or GameState.Finished || !turrets.Remove(turret))
            return new(false, "Select an active turret while the mission is open.", MessageKey: "SelectActiveTurret");
        Cash += turret.SellValue;
        SoundRequested?.Invoke(SoundCue.Build);
        return new(true, $"Turret recycled for ${turret.SellValue}.");
    }

    public bool StartNextWave()
    {
        if (State != GameState.Building) return false;
        Waves.StartNext(Map);
        State = GameState.Running;
        SoundRequested?.Invoke(SoundCue.WaveStart);
        return true;
    }

    public void TogglePause()
    {
        if (State == GameState.Finished) return;
        if (State == GameState.Paused) State = beforePause;
        else { beforePause = State; State = GameState.Paused; }
    }

    public void Update(float deltaTime)
    {
        if (State != GameState.Running || !float.IsFinite(deltaTime) || deltaTime <= 0) return;
        var remaining = Math.Min(deltaTime, .25f);
        while (remaining > 0 && State == GameState.Running)
        {
            var step = Math.Min(remaining, 1f / 60);
            Step(step);
            remaining -= step;
        }
    }

    private void Step(float deltaTime)
    {
        ElapsedSeconds += deltaTime;
        var spawned = Waves.Update(deltaTime);
        if (spawned is not null) aliens.Add(spawned);
        foreach (var alien in aliens)
        {
            alien.Advance(Map.Path, deltaTime);
            if (alien.Escaped)
            {
                Lives = Math.Max(0, Lives - alien.BreachDamage);
                SoundRequested?.Invoke(SoundCue.BaseHit);
            }
        }
        foreach (var turret in turrets) turret.Update(this, deltaTime);
        foreach (var projectile in projectiles) projectile.Update(this, deltaTime);
        projectiles.RemoveAll(projectile => projectile.Finished);
        foreach (var alien in aliens.Where(alien => !alien.IsAlive && !alien.Escaped))
        {
            Kills++;
            Cash += alien.Reward;
            Credits += Math.Max(1, alien.Reward / 3);
            impacts.Add(new Impact(alien.Position, alien.Radius + 8, ShotKind.Pulse));
        }
        aliens.RemoveAll(alien => !alien.IsAlive || alien.Escaped);
        foreach (var impact in impacts) impact.Lifetime -= deltaTime;
        impacts.RemoveAll(impact => impact.Lifetime <= 0);
        if (Lives <= 0) { Finish("Overrun"); return; }
        if (Waves.PendingCount == 0 && aliens.Count == 0)
        {
            CompletedWaves++;
            Cash += 60 + Waves.Wave * 8;
            Credits += 25;
            projectiles.Clear();
            impacts.Clear();
            State = GameState.Building;
            SoundRequested?.Invoke(SoundCue.WaveCleared);
        }
    }

    internal void Launch(Turret turret, Alien target, ShotKind kind)
    {
        projectiles.Add(new Projectile(turret.Position + turret.Facing * 23, target, turret.Damage, kind));
        SoundRequested?.Invoke(SoundCue.Shot);
    }

    internal void ResolveHit(Alien target, float damage, ShotKind kind)
    {
        if (kind == ShotKind.Plasma)
        {
            foreach (var alien in aliens.Where(alien => alien.IsTargetable && Vector2.Distance(alien.Position, target.Position) <= 75))
                alien.TakeDamage(damage);
        }
        else
        {
            target.TakeDamage(damage);
            if (kind == ShotKind.Cryo) target.ApplySlow(.45f, 2.5f);
        }
        impacts.Add(new Impact(target.Position, kind == ShotKind.Plasma ? 75 : 16, kind));
        SoundRequested?.Invoke(kind == ShotKind.Plasma ? SoundCue.Blast : SoundCue.Hit);
    }

    internal void FireRail(Turret turret, Alien target)
    {
        var direction = target.Position - turret.Position;
        if (direction.LengthSquared() < .01f) return;
        direction = Vector2.Normalize(direction);
        var hits = aliens.Where(alien => alien.IsTargetable).Select(alien => new
        {
            Alien = alien,
            Along = Vector2.Dot(alien.Position - turret.Position, direction),
            Across = Math.Abs((alien.Position.X - turret.Position.X) * direction.Y -
                (alien.Position.Y - turret.Position.Y) * direction.X)
        }).Where(hit => hit.Along >= 0 && hit.Along <= turret.Range && hit.Across <= hit.Alien.Radius + 8)
            .OrderByDescending(hit => ReferenceEquals(hit.Alien, target)).ThenBy(hit => hit.Along).Take(3);
        foreach (var hit in hits) hit.Alien.TakeDamage(turret.Damage);
        impacts.Add(new Impact(turret.Position, 2, ShotKind.Rail, turret.Position + direction * turret.Range));
        SoundRequested?.Invoke(SoundCue.Shot);
    }

    public void Retreat() => Finish("Retreated");
    private void Finish(string outcome)
    {
        if (State == GameState.Finished) return;
        State = GameState.Finished;
        Result = new RunResult(runId, DateTimeOffset.UtcNow, Waves.Wave, Kills, Credits,
            ElapsedSeconds, outcome, Map.Id);
        SoundRequested?.Invoke(outcome == "Overrun" ? SoundCue.Defeat : SoundCue.Retreat);
        RunFinished?.Invoke(Result);
    }
}
