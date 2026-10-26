using System.Numerics;
using AlienBreach.Core.Maps;
using AlienBreach.Core.Game;

namespace AlienBreach.Core.Entities;

public abstract class Alien(Vector2 position, float radius, float health, float speed, int reward, int breachDamage)
    : LivingEntity(position, radius, health)
{
    private float slowTime;
    private float slowMultiplier = 1;
    public abstract string Species { get; }
    public float Speed { get; } = speed;
    public int Reward { get; } = reward;
    public int BreachDamage { get; } = breachDamage;
    public float PathDistance { get; private set; }
    public Vector2 Facing { get; private set; } = Vector2.UnitX;
    public bool Escaped { get; private set; }
    public bool IsTargetable => IsAlive && !Escaped;
    public bool IsSlowed => slowTime > 0;
    protected virtual float MinimumSpeedMultiplier => .35f;

    public void Advance(EnemyPath path, float deltaTime)
    {
        if (!IsTargetable) return;
        var activeSlow = slowTime > 0 ? Math.Min(slowTime, deltaTime) : 0;
        PathDistance = Math.Min(path.Length, PathDistance + Speed *
            (activeSlow * slowMultiplier + (deltaTime - activeSlow)));
        slowTime = Math.Max(0, slowTime - deltaTime);
        if (slowTime == 0) slowMultiplier = 1;
        Position = path.PositionAt(PathDistance);
        Facing = path.DirectionAt(PathDistance);
        Escaped = PathDistance >= path.Length;
    }

    public void ApplySlow(float multiplier, float seconds)
    {
        if (seconds <= 0 || multiplier >= 1) return;
        slowMultiplier = Math.Min(slowMultiplier, Math.Clamp(multiplier, MinimumSpeedMultiplier, 1));
        slowTime = Math.Max(slowTime, seconds);
    }
}

public sealed class Stalker(Vector2 position, int wave)
    : Alien(position, 15, EnemyDifficulty.ScaleHealth(28f + wave * 5f, wave), 62 + Math.Min(wave, 11) * 2, 10, 1)
{
    public override string Species => "STALKER";
}

public sealed class Spitter(Vector2 position, int wave)
    : Alien(position, 18, EnemyDifficulty.ScaleHealth(42f + wave * 7f, wave), 82 + Math.Min(wave, 15), 15, 2)
{
    public override string Species => "SPITTER";
}

public sealed class Brute(Vector2 position, int wave)
    : Alien(position, 24, EnemyDifficulty.ScaleHealth(88f + wave * 12f, wave), 43, 28, 3)
{
    public override string Species => "BRUTE";
}

public sealed class RedTitan(Vector2 position, int wave)
    : Alien(position, 36, EnemyDifficulty.ScaleHealth(360f + wave * 45f, wave), 38, 90, 6)
{
    public override string Species => "RED TITAN";
    protected override float MinimumSpeedMultiplier => .7f;
}
