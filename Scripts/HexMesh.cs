using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class HexMesh : MeshInstance3D
{
    private SurfaceTool surfaceTool = new SurfaceTool();

    List<Vector3> vertices = [];
    List<Vector3> triangles = [];

    public void Begin()
    {
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        surfaceTool.SetCustomFormat(0, SurfaceTool.CustomFormat.RgbaFloat);
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
        var center = cell.Position;

        surfaceTool.SetColor(cell.Color);
        for (var i = 0; i < 6; i++)
        {
            var v2 = center + HexMetrics.Corners[i + 1];
            var v3 = center + HexMetrics.Corners[i];

            AddTriangle(v1: center, v2, v3, cell.Color);
        }
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color color)
    {
        surfaceTool.SetColor(color);
        surfaceTool.AddVertex(v1);
        surfaceTool.SetColor(color);
        surfaceTool.AddVertex(v2);
        surfaceTool.SetColor(color);
        surfaceTool.AddVertex(v3);
    }
}
