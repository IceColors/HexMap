using System.Collections.Generic;
using Godot;

namespace HexMap.Scripts;

[GlobalClass]
public partial class HexMesh : MeshInstance3D
{
    private SurfaceTool surfaceTool = new();

    public void Begin()
    {
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        surfaceTool.SetCustomFormat(0,
                                    SurfaceTool.CustomFormat.RgbaFloat);
        // Flat normals
        surfaceTool.SetSmoothGroup(uint.MaxValue);
    }

    public void Triangulate(IReadOnlyCollection<HexCell> cells)
    {
        Begin();
        foreach (var cell in cells)
        {
            Triangulate(cell);
        }

        End();
    }

    public void End()
    {
        surfaceTool.GenerateNormals();
        //surfaceTool.GenerateTangents();

        Mesh = surfaceTool.Commit();
        CreateTrimeshCollision();
    }

    private void Triangulate(HexCell cell)
    {
        for (var direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
        {
            Triangulate(direction, cell);
        }
    }

    private void Triangulate(HexDirection direction, HexCell cell)
    {
        var center = cell.Position;
        var v1 = center + HexMetrics.GetFirstSolidCorner(direction);
        var v2 = center + HexMetrics.GetSecondSolidCorner(direction);

        AddTriangle(center, v1, v2, cell.Color, cell.Color, cell.Color);

        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, v1, v2);
        }
    }

    private void TriangulateConnection(HexDirection direction, HexCell cell, Vector3 v1, Vector3 v2)
    {
        var neighbor = cell.GetNeighbor(direction);
        if (neighbor is null)
        {
            return;
        }

        var bridge = HexMetrics.GetBridge(direction);
        var v3 = v1 + bridge;
        var v4 = v2 + bridge;

        AddQuad(v1, v2, v3, v4,
                cell.Color, cell.Color, neighbor.Color, neighbor.Color);

        var nextNeighbor = cell.GetNeighbor(direction.Next());

        if (direction <= HexDirection.E && nextNeighbor is not null)
        {
            AddTriangle(v3, v1, v1 + HexMetrics.GetBridge(direction.Next()),
            neighbor.Color, cell.Color, nextNeighbor.Color);
        }
    }

    private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
                         Color c1, Color c2, Color c3, Color c4)
    {
        surfaceTool.AddTriangleFan(
            [v2, v1, v3, v4],
            colors: [c2, c1, c3, c4]);
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color c1, Color c2, Color c3)
    {
        surfaceTool.AddTriangleFan(
            [v1, v2, v3],
            colors: [c1, c2, c3]);
    }

    protected override void Dispose(bool disposing)
    {
        surfaceTool.Dispose();
        base.Dispose(disposing);
    }
}
