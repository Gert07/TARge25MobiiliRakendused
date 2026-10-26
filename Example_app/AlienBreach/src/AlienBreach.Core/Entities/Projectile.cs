using System.Numerics;
using AlienBreach.Core.Game;
using AlienBreach.Core.Turrets;

namespace AlienBreach.Core.Entities;

public sealed class Projectile(Vector2 position, Alien target, float damage, ShotKind kind) : Entity(position, 4)
{
    public Alien Target { get; } = target;
    public float Damage { get; } = damage;
    public ShotKind Kind { get; } = kind;
    public bool Finished { get; private set; }
    private float lifetime = 3;

    internal void Update(GameEngine game, float deltaTime)
    {
        lifetime -= deltaTime;
        if (!Target.IsTargetable || lifetime <= 0) { Finished = true; return; }
        var offset = Target.Position - Position;
        var speed = Kind == ShotKind.Plasma ? 390 : 680;
        if (offset.Length() <= speed * deltaTime + Target.Radius)
        {
            game.ResolveHit(Target, Damage, Kind);
            Finished = true;
        }
        else Position += Vector2.Normalize(offset) * speed * deltaTime;
    }
}

public sealed class Impact(Vector2 position, float radius, ShotKind kind, Vector2? end = null) : Entity(position, radius)
{
    public ShotKind Kind { get; } = kind;
    public Vector2? End { get; } = end;
    public float Lifetime { get; internal set; } = .3f;
}
