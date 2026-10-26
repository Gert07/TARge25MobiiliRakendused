using System.Numerics;
using AlienBreach.Core.Entities;

namespace AlienBreach.Core.Game;

public static class EnemyFactory
{
    public static IReadOnlyList<Alien> CreateWave(int wave, Vector2 spawn)
    {
        if (wave < 1) throw new ArgumentOutOfRangeException(nameof(wave));
        var enemies = new List<Alien>();
        // A guaranteed titan leads wave 6, and every subsequent wave.
        if (wave >= 6)
            for (var i = 0; i < 1 + (wave - 6) / 4; i++) enemies.Add(new RedTitan(spawn, wave));
        for (var i = 0; i < 6 + wave * 2; i++)
            enemies.Add(wave >= 3 && i % 6 == 4 ? new Brute(spawn, wave)
                : wave >= 2 && i % 4 == 2 ? new Spitter(spawn, wave) : new Stalker(spawn, wave));
        return enemies.AsReadOnly();
    }
}
