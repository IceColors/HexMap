using Godot;
using System;

namespace HexMap.Scripts;

public readonly struct EdgeVertices : IEquatable<EdgeVertices>
{
    public Vector3 V1 { get; }
    public Vector3 V2 { get; }
    public Vector3 V3 { get; }
    public Vector3 V4 { get; }
    public EdgeVertices(Vector3 corner1, Vector3 corner2)
    {
        V1 = corner1;
        V2 = corner1.Lerp(corner2, 1f / 3f);
        V3 = corner1.Lerp(corner2, 2f / 3f);
        V4 = corner2;
    }

    public EdgeVertices(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        V1 = v1;
        V2 = v2;
        V3 = v3;
        V4 = v4;
    }

    public static EdgeVertices TerraceLerp(EdgeVertices a, EdgeVertices b, int step)
    {
        return new EdgeVertices(
            HexMetrics.TerraceLerp(a.V1, b.V1, step),
            HexMetrics.TerraceLerp(a.V2, b.V2, step),
            HexMetrics.TerraceLerp(a.V3, b.V3, step),
            HexMetrics.TerraceLerp(a.V4, b.V4, step)
        );
    }

    #region Equality

    public override bool Equals(object obj)
        => obj is EdgeVertices other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(V1, V2, V3, V4);

    public static bool operator ==(EdgeVertices left, EdgeVertices right)
        => left.Equals(right);

    public static bool operator !=(EdgeVertices left, EdgeVertices right)
        => !(left == right);

    public bool Equals(EdgeVertices other)
        => other.V1 == V1 && other.V2 == V2 && other.V3 == V3 && other.V4 == V4;

    #endregion
}