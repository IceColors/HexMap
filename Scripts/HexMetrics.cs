using Godot;

public static class HexMetrics
{
    public const float OUTER_RADIUS = 10;
    public const float INNER_RADIUS = OUTER_RADIUS * 0.866025404f;

    public static Vector3[] Corners { get; } =
        [
            new(0, 0, OUTER_RADIUS),
            new(INNER_RADIUS, 0, 0.5f * OUTER_RADIUS),
            new(INNER_RADIUS, 0, -0.5f * OUTER_RADIUS),
            new(0, 0, -OUTER_RADIUS),
            new(-INNER_RADIUS, 0, -0.5f * OUTER_RADIUS),
            new(-INNER_RADIUS, 0, 0.5f * OUTER_RADIUS),
            new(0, 0, OUTER_RADIUS),
        ];
}
