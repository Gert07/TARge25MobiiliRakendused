using System.Numerics;
using AlienBreach.Core.Entities;
using AlienBreach.Core.Game;

namespace AlienBreach.Core.Turrets;

public abstract class Turret : Entity
{
    private float cooldown;
    private readonly float globalDamageMultiplier;
    protected Turret(TurretType type, Vector2 position, int firepowerLevel, ITargetingStrategy? targeting = null)
        : base(position, 23)
    {
        Definition = TurretCatalog.Get(type);
        InvestedCash = Definition.Cost;
        globalDamageMultiplier = 1 + firepowerLevel * .08f;
        Targeting = targeting ?? new FirstTargeting();
    }

    public TurretDefinition Definition { get; }
    public ITargetingStrategy Targeting { get; }
    public int Level { get; private set; } = 1;
    public int InvestedCash { get; private set; }
    public int SellValue => (int)(InvestedCash * .7f);
    public int UpgradeCost => (int)(Definition.Cost * (.7f + .4f * (Level - 1)));
    public float Range => Definition.Range + (Level - 1) * 16;
    public float Damage => Definition.Damage * (1 + .5f * (Level - 1)) * globalDamageMultiplier;
    public Vector2 Facing { get; private set; } = -Vector2.UnitY;

    internal void Upgrade()
    {
        InvestedCash += UpgradeCost;
        Level++;
    }

    internal void Update(GameEngine game, float deltaTime)
    {
        cooldown = Math.Max(0, cooldown - deltaTime);
        var target = Targeting.Select(game.Aliens, Position, Range);
        if (target is null) return;
        var direction = target.Position - Position;
        if (direction.LengthSquared() > .01f) Facing = Vector2.Normalize(direction);
        if (cooldown > 0) return;
        cooldown = Definition.Interval / (1 + (Level - 1) * .15f);
        Attack(game, target);
    }

    protected abstract void Attack(GameEngine game, Alien target);
}

public sealed class PulseTurret(Vector2 position, int firepowerLevel = 0) : Turret(TurretType.Pulse, position, firepowerLevel)
{
    protected override void Attack(GameEngine game, Alien target) => game.Launch(this, target, ShotKind.Pulse);
}

public sealed class RepeaterTurret(Vector2 position, int firepowerLevel = 0) : Turret(TurretType.Repeater, position, firepowerLevel)
{
    protected override void Attack(GameEngine game, Alien target) => game.Launch(this, target, ShotKind.Repeater);
}

public sealed class PlasmaTurret(Vector2 position, int firepowerLevel = 0) : Turret(TurretType.Cannon, position, firepowerLevel)
{
    protected override void Attack(GameEngine game, Alien target) => game.Launch(this, target, ShotKind.Plasma);
}

public sealed class CryoTurret(Vector2 position, int firepowerLevel = 0) : Turret(TurretType.Cryo, position, firepowerLevel)
{
    protected override void Attack(GameEngine game, Alien target) => game.Launch(this, target, ShotKind.Cryo);
}

public sealed class RailgunTurret(Vector2 position, int firepowerLevel = 0)
    : Turret(TurretType.Railgun, position, firepowerLevel, new StrongestTargeting())
{
    protected override void Attack(GameEngine game, Alien target) => game.FireRail(this, target);
}

public static class TurretFactory
{
    public static Turret Create(TurretType type, Vector2 position, int firepowerLevel) => type switch
    {
        TurretType.Pulse => new PulseTurret(position, firepowerLevel),
        TurretType.Repeater => new RepeaterTurret(position, firepowerLevel),
        TurretType.Cannon => new PlasmaTurret(position, firepowerLevel),
        TurretType.Cryo => new CryoTurret(position, firepowerLevel),
        TurretType.Railgun => new RailgunTurret(position, firepowerLevel),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
