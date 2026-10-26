using AlienBreach.Core.Entities;
using AlienBreach.Core.Maps;

namespace AlienBreach.Core.Game;

public sealed class WaveDirector
{
    private readonly Queue<Alien> pending = new();
    private float cooldown;
    public int Wave { get; private set; }
    public int PendingCount => pending.Count;

    internal void StartNext(BattleMap map)
    {
        Wave++;
        foreach (var alien in EnemyFactory.CreateWave(Wave, map.Path.Points[0])) pending.Enqueue(alien);
        cooldown = 0;
    }

    internal Alien? Update(float deltaTime)
    {
        cooldown -= deltaTime;
        if (pending.Count == 0 || cooldown > 0) return null;
        cooldown = Math.Max(.3f, .85f - Wave * .025f);
        return pending.Dequeue();
    }
}
