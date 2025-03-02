using System;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace HexMap.Scripts;

public readonly struct HexCoordinates : IEquatable<HexCoordinates>
{
    private HexCoordinates(int x, int z)
    {
        X = x;
        Z = z;
    }

    public int X { get; }
    public int Z { get; }
    public int Y => -X - Z;

    internal static HexCoordinates FromOffsetCoordinates(int x, int z)
    {
        return new HexCoordinates(x - (z / 2), z);
    }

    internal static HexCoordinates FromPosition(Vector3 position)
    {
        var x = position.X / (HexMetrics.InnerRadius * 2f);
        var y = -x;

        var offset = position.Z / (HexMetrics.OuterRadius * 3f);
        x -= offset;
        y -= offset;

        var iX = Mathf.RoundToInt(x);
        var iY = Mathf.RoundToInt(y);
        var iZ = Mathf.RoundToInt(-x - y);

        if (iX + iY + iZ != 0)
        {
            // wrong rounding
            var dX = Mathf.Abs(x - iX);
            var dY = Mathf.Abs(y - iY);
            var dZ = Mathf.Abs(-x - y - iZ);
            if (dX > dY && dX > dZ)
            {
                iX = -iY - iZ;
            }
            else if (dZ > dY)
            {
                iZ = -iX - iY;
            }
        }

        return new HexCoordinates(iX, iZ);
    }

    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }

    public string ToStringOnSeparateLines()
    {
        return $"{X}\n{Y}\n{Z}";
    }

    #region Equality
    public override bool Equals([NotNullWhen(true)] object obj)
        => obj is HexCoordinates other && Equals(other);

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Z);
    }

    public bool Equals(HexCoordinates other) => X == other.X && Z == other.Z;
    public static bool operator ==(HexCoordinates left, HexCoordinates right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(HexCoordinates left, HexCoordinates right)
    {
        return !(left == right);
    }
    #endregion
}
