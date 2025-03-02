using Godot;

namespace HexMap.Scripts;

[GlobalClass]
public partial class HexCell : Node3D
{
    [Export]
    public Label3D CoordinatesLabel { get; set; }

    public HexCoordinates Coordinates { get; set; }
    public Color Color { get; internal set; } = Colors.LightGoldenrod;

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

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
