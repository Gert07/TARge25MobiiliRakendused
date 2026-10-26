namespace AlienBreach.Core.Turrets;

public enum TurretType { Pulse, Repeater, Cannon, Cryo, Railgun }
public enum ShotKind { Pulse, Repeater, Plasma, Cryo, Rail }

public sealed record TurretDefinition(TurretType Type, string Name, string ShortName, string Description,
    int Cost, float Range, float Damage, float Interval, ShotKind ShotKind);

public static class TurretCatalog
{
    public static IReadOnlyList<TurretDefinition> All { get; } = Array.AsReadOnly(new[]
    {
        new TurretDefinition(TurretType.Pulse, "Pulse turret", "PULSE", "Balanced single-target defense.", 90, 165, 18, .45f, ShotKind.Pulse),
        new TurretDefinition(TurretType.Repeater, "Rapid repeater", "REPEATER", "Fast fire for packs of light aliens.", 150, 150, 8, .14f, ShotKind.Repeater),
        new TurretDefinition(TurretType.Cannon, "Plasma cannon", "PLASMA", "Explodes in a 75-unit radius.", 180, 190, 48, 1.2f, ShotKind.Plasma),
        new TurretDefinition(TurretType.Cryo, "Cryo emitter", "CRYO", "Slows aliens by 55% for 2.5 seconds.", 120, 155, 8, .6f, ShotKind.Cryo),
        new TurretDefinition(TurretType.Railgun, "Railgun", "RAILGUN", "Long range; targets the strongest, piercing up to 3 aliens.", 240, 330, 115, 1.65f, ShotKind.Rail)
    });

    public static TurretDefinition Get(TurretType type) => All.First(definition => definition.Type == type);
}
