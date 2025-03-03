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
    public const float SolidFactor = 0.8f;
    public const float BlendFactor = 1f - SolidFactor;
    public const float ElevationStep = 3f;
    public const int TerracesPerSlope = 2;
    public const int TerraceSteps = (TerracesPerSlope * 2) + 1;
    public const float HorizontalTerraceStepsSize = 1f / TerraceSteps;
    public const float VerticalTerraceStepsSize = 1f / (TerracesPerSlope + 1);
    public const float NoiseScale = 10f;
    public const float CellPerturbStrength = 0.8f;
    public const float ElevationPerturbStrength = 1.5f;

    private static readonly FastNoiseLite[] noiseGenerators = [
        new FastNoiseLite(),
        new FastNoiseLite(),
        new FastNoiseLite(),
        new FastNoiseLite(),
    ];

    public static void InitializeNoiseGenerators()
    {
        var i = 0;
        foreach (var noiseGenerator in noiseGenerators)
        {
            noiseGenerator.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
            noiseGenerator.Seed = i++;
            noiseGenerator.Frequency = 0.025f;
            noiseGenerator.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            noiseGenerator.FractalOctaves = 2;
            noiseGenerator.FractalLacunarity = 2;
            noiseGenerator.FractalGain = 0.5f;
            noiseGenerator.FractalWeightedStrength = 0.8f;
        }
    }

    public static Vector4 SampleNoise(Vector3 position)
    {
        var sample = new Vector4();
        for (var i = 0; i < noiseGenerators.Length; i++)
        {
            sample[i] = noiseGenerators[i].GetNoise2D(
                position.X * NoiseScale, position.Z * NoiseScale);
        }
        if (!sample.IsEqualApprox(Vector4.Zero))
        {
            
        }
        return sample;
    }

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
