using System.Numerics;

namespace AlienBreach.Core.Entities;

public abstract class Entity(Vector2 position, float radius)
{
    public Vector2 Position { get; internal set; } = position;
    public float Radius { get; } = radius;
}

public abstract class LivingEntity(Vector2 position, float radius, float health) : Entity(position, radius)
{
    public float MaxHealth { get; } = health;
    public float Health { get; private set; } = health;
    public bool IsAlive => Health > 0;

    public void TakeDamage(float damage)
    {
        if (damage > 0) Health = Math.Max(0, Health - damage);
    }

    public void Heal(float amount)
    {
        if (amount > 0 && IsAlive) Health = Math.Min(MaxHealth, Health + amount);
    }
}
