namespace AlienBreach.Core.Game;

public static class EnemyDifficulty
{
    public static float HealthMultiplier(int wave)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(wave, 1);
        // Wave 11 adds 20%, wave 12 adds another 20%, and so on.
        // Keep extremely late endless waves finite rather than overflowing health.
        return (float)Math.Min(1.5 * Math.Pow(1.2, Math.Max(0, wave - 10)), float.MaxValue / 16);
    }

    public static float ScaleHealth(float originalHealth, int wave) =>
        (float)Math.Min((double)originalHealth * HealthMultiplier(wave), float.MaxValue / 4);
}
