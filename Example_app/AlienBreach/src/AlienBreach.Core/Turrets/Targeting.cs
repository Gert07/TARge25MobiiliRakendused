using System.Numerics;
using AlienBreach.Core.Entities;

namespace AlienBreach.Core.Turrets;

public interface ITargetingStrategy
{
    Alien? Select(IEnumerable<Alien> aliens, Vector2 origin, float range);
}

public sealed class FirstTargeting : ITargetingStrategy
{
    public Alien? Select(IEnumerable<Alien> aliens, Vector2 origin, float range) =>
        aliens.Where(alien => alien.IsTargetable && Vector2.DistanceSquared(origin, alien.Position) <= range * range)
            .MaxBy(alien => alien.PathDistance);
}

public sealed class StrongestTargeting : ITargetingStrategy
{
    public Alien? Select(IEnumerable<Alien> aliens, Vector2 origin, float range) =>
        aliens.Where(alien => alien.IsTargetable && Vector2.DistanceSquared(origin, alien.Position) <= range * range)
            .OrderByDescending(alien => alien.MaxHealth).ThenByDescending(alien => alien.PathDistance).FirstOrDefault();
}
