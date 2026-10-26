using System.Numerics;

namespace AlienBreach.Core.Maps;

public enum MapTheme { Outpost, Reactor, Cryo }

public sealed record BattleMap(string Id, string Name, string Description, string Difficulty,
    MapTheme Theme, EnemyPath Path, int StartingCash = 350)
{
    public const float Width = 1100;
    public const float Height = 620;

    public bool CanBuildAt(Vector2 position, float radius) =>
        float.IsFinite(position.X) && float.IsFinite(position.Y) &&
        position.X >= radius + 12 && position.Y >= radius + 12 &&
        position.X <= Width - radius - 12 && position.Y <= Height - radius - 12 &&
        Path.DistanceTo(position) >= EnemyPath.RoadWidth / 2 + radius + 8;
}

public static class MapCatalog
{
    public static IReadOnlyList<BattleMap> All { get; } = Array.AsReadOnly(new[]
    {
        new BattleMap("outpost-07", "Outpost 07", "A winding cargo lane. Overlapping bends reward careful placement.",
            "STANDARD", MapTheme.Outpost, new EnemyPath(new(0, 145), new(260, 145), new(260, 450),
                new(570, 450), new(570, 200), new(850, 200), new(850, 470), new(1100, 470))),
        new BattleMap("reactor-ring", "Reactor Ring", "A long spiral into the reactor core. Defend the inner chamber.",
            "FORGIVING", MapTheme.Reactor, new EnemyPath(new(0, 90), new(980, 90), new(980, 530),
                new(120, 530), new(120, 250), new(760, 250), new(760, 400), new(460, 400))),
        new BattleMap("cryo-crossing", "Cryo Crossing", "A short diagonal ice passage. Slows and long sightlines matter.",
            "CHALLENGING", MapTheme.Cryo, new EnemyPath(new(0, 500), new(220, 500),
                new(420, 130), new(650, 130), new(850, 500), new(1100, 500)))
    });

    public static BattleMap Get(string id) => All.First(map => map.Id == id);
    public static string NameOf(string id) => All.FirstOrDefault(map => map.Id == id)?.Name ?? "Legacy survival";
}
