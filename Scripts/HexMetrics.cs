using Godot;
using System;
using System.Collections.ObjectModel;

namespace HexMap.Scripts;

public enum HexEdgeType
{
    Flat,
    Slope,
    Cliff
}

public static class HexMetrics
{
    public const float OuterRadius = 10;
    public const float InnerRadius = OuterRadius * 0.866025404f;
    public const float SolidFactor = 0.75f;
    public const float BlendFactor = 1f - SolidFactor;
    public const float ElevationStep = 5f;
    public const int TerracesPerSlope = 2;
    public const int TerraceSteps = (TerracesPerSlope * 2) + 1;
    public const float HorizontalTerraceStepsSize = 1f / TerraceSteps;
    public const float VerticalTerraceStepsSize = 1f / (TerracesPerSlope + 1);

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
        return Corners[(int)direction];
    }

    public static Vector3 GetSecondCorner(HexDirection direction)
    {
        return Corners[(int)direction + 1];
    }

    public static Vector3 GetFirstSolidCorner(HexDirection direction)
    {
        return Corners[(int)direction] * SolidFactor;
    }

    public static Vector3 GetSecondSolidCorner(HexDirection direction)
    {
        return Corners[(int)direction + 1] * SolidFactor;
    }

    public static Vector3 GetBridge(HexDirection direction)
    {
        return (Corners[(int)direction] + Corners[(int)direction + 1]) * BlendFactor;
    }

    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        var h = step * HorizontalTerraceStepsSize;
        a.X += (b.X - a.X) * h;
        a.Z += (b.Z - a.Z) * h;
        var v = VerticalTerraceStepsSize * ((step + 1) / 2);
        a.Y += (b.Y - a.Y) * v;
        return a;
    }

    public static Color TerraceLerp(Color a, Color b, int step)
    {
        var h = step * HorizontalTerraceStepsSize;
        return a.Lerp(b, h);
    }

    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2)
        {
            return HexEdgeType.Flat;
        }

        if (Math.Abs(elevation2 - elevation1) == 1)
        {
            return HexEdgeType.Slope;
        }

        return HexEdgeType.Cliff;
    }
}
