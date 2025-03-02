using System.Collections.ObjectModel;
using Godot;

namespace HexMap.Scripts;

[GlobalClass]
public partial class HexGrid : Node3D
{
    [Export]
    public PackedScene HexCellScene { get; set; }

    [Export]
    public HexMesh MeshInstance { get; set; }

    [Export]
    public Color TouchedColor { get; set; } = Colors.AliceBlue;

    public int Width { get; set; } = 6;
    public int Height { get; set; } = 6;

    public Collection<HexCell> Cells { get; } = [];

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        for (int z = 0, i = 0; z < Height; z++)
        {
            for (var x = 0; x < Width; x++)
            {
                CreateCell(x, z, i++);
            }
        }

        MeshInstance.Triangulate(Cells);
    }

    void CreateCell(int x, int z, int i)
    {
        var posX = (x + (z * 0.5f) - (z / 2)) * (HexMetrics.InnerRadius * 2f);
        var posZ = z * (HexMetrics.OuterRadius * 1.5f);
        var position = new Vector3(posX, 0, posZ);
        var cell = HexCellScene.Instantiate<HexCell>();
        Cells.Add(cell);
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.Position = position;
        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, Cells[i - 1]);
        }

        if (z > 0)
        {
            if (int.IsEvenInteger(z))
            {
                cell.SetNeighbor(HexDirection.SE, Cells[i - Width]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, Cells[i - Width - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, Cells[i - Width]);
                if (x < Width - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, Cells[i - Width + 1]);
                }
            }
        }

        AddChild(cell);
    }

    public void ColorCell(HexCell cell, Color color)
    {
        cell.Color = color;
        MeshInstance.Triangulate(Cells);
    }

    public HexCell GetCellFromRay(PhysicsRayQueryParameters3D rayQuery)
    {
        var spaceState = GetWorld3D().DirectSpaceState;

        var result = spaceState.IntersectRay(rayQuery);
        if (result.Count > 0)
        {
            var hitMesh = new MeshInstance3D();
            hitMesh.Mesh = new SphereMesh()
            {
                Material = new StandardMaterial3D()
                {
                    AlbedoColor = Colors.DarkOrange,
                },
                Radius = 1f,
            };
            hitMesh.Position = (Vector3)result["position"];
            AddChild(hitMesh);
            var cell = GetCell((Vector3)result["position"]);
            return cell;
        }

        return null;
    }

    private HexCell GetCell(Vector3 position)
    {
        // Global to local
        var inverseTransformPoint = position * GlobalTransform;

        var coordinates = HexCoordinates.FromPosition(inverseTransformPoint);

        return GetCell(coordinates);
    }

    private HexCell GetCell(HexCoordinates coordinates)
    {
        var z = coordinates.Z;
        var x = coordinates.X + (z / 2);

        if (z < 0 || x < 0)
        {
            return null;
        }

        var ind = x + (z * Width);
        if (ind < Cells.Count)
        {
            return Cells[ind];
        }

        return null;
    }
}
