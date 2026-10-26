using System.Numerics;

namespace AlienBreach.Core.Maps;

public sealed class EnemyPath
{
    private readonly Vector2[] points;
    private readonly float[] lengths;
    public IReadOnlyList<Vector2> Points { get; }
    public float Length { get; }
    public const float RoadWidth = 56;

    public EnemyPath(params Vector2[] points)
    {
        if (points.Length < 2 || points.Any(p => !float.IsFinite(p.X) || !float.IsFinite(p.Y)))
            throw new ArgumentException("A route needs at least two finite points.", nameof(points));
        this.points = points.ToArray();
        Points = Array.AsReadOnly(this.points);
        lengths = new float[points.Length - 1];
        for (var i = 0; i < lengths.Length; i++)
        {
            lengths[i] = Vector2.Distance(points[i], points[i + 1]);
            if (lengths[i] < 1) throw new ArgumentException("Route segments must have a length.", nameof(points));
            Length += lengths[i];
        }
    }

    public Vector2 PositionAt(float distance)
    {
        distance = Math.Max(0, distance);
        for (var i = 0; i < lengths.Length; i++)
        {
            if (distance <= lengths[i]) return Vector2.Lerp(points[i], points[i + 1], distance / lengths[i]);
            distance -= lengths[i];
        }
        return points[^1];
    }

    public Vector2 DirectionAt(float distance)
    {
        for (var i = 0; i < lengths.Length; i++)
        {
            if (distance <= lengths[i]) return Vector2.Normalize(points[i + 1] - points[i]);
            distance -= lengths[i];
        }
        return Vector2.Normalize(points[^1] - points[^2]);
    }

    public float DistanceTo(Vector2 point)
    {
        var best = float.MaxValue;
        for (var i = 0; i < lengths.Length; i++)
        {
            var segment = points[i + 1] - points[i];
            var t = Math.Clamp(Vector2.Dot(point - points[i], segment) / segment.LengthSquared(), 0, 1);
            best = Math.Min(best, Vector2.Distance(point, points[i] + segment * t));
        }
        return best;
    }
}
