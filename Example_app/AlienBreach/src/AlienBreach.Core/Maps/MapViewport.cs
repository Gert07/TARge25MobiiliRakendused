using System.Numerics;

namespace AlienBreach.Core.Maps;

// Drawing and touch input share the same transform for both previews and full-screen gameplay.
public readonly record struct MapViewport(float X, float Y, float ScaleX, float ScaleY)
{
    public MapViewport(float x, float y, float scale) : this(x, y, scale, scale) { }
    public float Scale => Math.Min(ScaleX, ScaleY);

    public static MapViewport Fit(float width, float height)
    {
        var scale = Math.Min(width / BattleMap.Width, height / BattleMap.Height);
        return new((width - BattleMap.Width * scale) / 2, (height - BattleMap.Height * scale) / 2, scale);
    }

    public static MapViewport Fill(float width, float height) =>
        new(0, 0, width / BattleMap.Width, height / BattleMap.Height);

    // Keep the battlefield 20% smaller and leave the lower fifth for controls.
    public static MapViewport Gameplay(float width, float height) =>
        new(width * .1f, 0, width * .8f / BattleMap.Width, height * .8f / BattleMap.Height);

    public Vector2 ToScreen(Vector2 world) => new(X + world.X * ScaleX, Y + world.Y * ScaleY);

    public Vector2? ToWorld(Vector2 point)
    {
        if (ScaleX <= 0 || ScaleY <= 0 || !float.IsFinite(ScaleX) || !float.IsFinite(ScaleY)) return null;
        var world = (point - new Vector2(X, Y)) / new Vector2(ScaleX, ScaleY);
        return world.X is >= 0 and <= BattleMap.Width && world.Y is >= 0 and <= BattleMap.Height ? world : null;
    }
}
