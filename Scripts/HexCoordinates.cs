using System;
using Godot;

public readonly struct HexCoordinates
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
        var x = position.X / (HexMetrics.INNER_RADIUS * 2f);
        var y = -x;

        var offset = position.Z / (HexMetrics.OUTER_RADIUS * 3f);
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
}
