using Godot;
using System.Collections.ObjectModel;

namespace HexMap.Scripts;

public static class HexMetrics
{
    public const float OuterRadius = 10;
    public const float InnerRadius = OuterRadius * 0.866025404f;

    public const float SolidFactor = 0.75f;

    public const float BlendFactor = 1f - SolidFactor;

    private static Collection<Vector3> Corners { get; } =
        [
            new(0, 0, OuterRadius),
            new(InnerRadius, 0, 0.5f * OuterRadius),
            new(InnerRadius, 0, -0.5f * OuterRadius),
            new(0, 0, -OuterRadius),
            new(-InnerRadius, 0, -0.5f * OuterRadius),
            new(-InnerRadius, 0, 0.5f * OuterRadius),
            new(0, 0, OuterRadius),
        ];

    public static Vector3 GetFirstCorner(HexDirection direction)
    {
        return Corners[(int)direction + 1];
    }

    public static Vector3 GetSecondCorner(HexDirection direction)
    {
        return Corners[(int)direction];
    }

    public static Vector3 GetFirstSolidCorner(HexDirection direction)
    {
        return Corners[(int)direction + 1] * SolidFactor;
    }

    public static Vector3 GetSecondSolidCorner(HexDirection direction)
    {
        return Corners[(int)direction] * SolidFactor;
    }

    public static Vector3 GetBridge(HexDirection direction)
    {
        return (Corners[(int)direction] + Corners[(int)direction + 1]) * BlendFactor;
    }
}
