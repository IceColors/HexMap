using Godot;
using System;

namespace HexMap.Scripts;

[GlobalClass]
public partial class HexCell : Node3D
{
    [Export]
    public Label3D CoordinatesLabel { get; set; }

    public HexCoordinates Coordinates { get; set; }
    public Color Color { get; internal set; } = Colors.LightGoldenrod;

    private int elevation;
    public int Elevation
    {
        get => elevation; set
        {
            elevation = value;
            Position = Position with { Y = value * HexMetrics.ElevationStep };
            Position = Position with
            {
                Y = Position.Y
                    + (HexMetrics.SampleNoise(Position).Y
                    * HexMetrics.ElevationPerturbStrength)
            };
        }
    }

    private HexCell[] neighbors = new HexCell[6];

    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        CoordinatesLabel.Text = Coordinates.ToStringOnSeparateLines();
    }

    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(
            elevation,
            GetNeighbor(direction).elevation);
    }

    internal HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }
}
