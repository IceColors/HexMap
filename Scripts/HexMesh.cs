using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace HexMap.Scripts;

[GlobalClass]
public partial class HexMesh : MeshInstance3D
{
    private SurfaceTool surfaceTool = new();

    public void Begin()
    {
        HexMetrics.InitializeNoiseGenerators();
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

        // Clear previous collision
        GetChildren().OfType<StaticBody3D>()?.FirstOrDefault()?.QueueFree();
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
        var e = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction)
        );

        TriangulateEdgeFan(center, e, cell.Color);

        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, e);
        }
    }

    private void TriangulateConnection(HexDirection direction, HexCell cell,
        EdgeVertices e1)
    {
        var neighbor = cell.GetNeighbor(direction);
        if (neighbor is null)
        {
            return;
        }

        var bridge = HexMetrics.GetBridge(direction);
        bridge.Y = neighbor.Position.Y - cell.Position.Y;
        var e2 = new EdgeVertices(
            e1.V1 + bridge,
            e1.V4 + bridge
        );

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(
                e1,
                cell,
                e2,
                neighbor);
        }
        else
        {
            TriangulateEdgeStrip(e1, cell.Color, e2, neighbor.Color);
        }

        var nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor is not null)
        {
            var v5 = e1.V4 + HexMetrics.GetBridge(direction.Next());
            v5.Y = nextNeighbor.Position.Y;

            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(
                        e1.V4, cell,
                        e2.V4, neighbor,
                        v5, nextNeighbor);
                }
                else
                {
                    TriangulateCorner(
                        v5, nextNeighbor,
                        e1.V4, cell,
                        e2.V4, neighbor);
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(
                    e2.V4, neighbor,
                    v5, nextNeighbor,
                    e1.V4, cell);
            }
            else
            {
                TriangulateCorner(
                    v5, nextNeighbor,
                    e1.V4, cell,
                    e2.V4, neighbor);
            }
        }
    }

    private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        AddTriangle(center, edge.V1, edge.V2, color);
        AddTriangle(center, edge.V2, edge.V3, color);
        AddTriangle(center, edge.V3, edge.V4, color);
    }

    private void TriangulateEdgeStrip(EdgeVertices e1, Color c1,
                                      EdgeVertices e2, Color c2)
    {
        AddQuad(e1.V1, e1.V2, e2.V1, e2.V2, c1, c2);
        AddQuad(e1.V2, e1.V3, e2.V2, e2.V3, c1, c2);
        AddQuad(e1.V3, e1.V4, e2.V3, e2.V4, c1, c2);

    }

    private void TriangulateCorner(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        var leftEdgeType = bottomCell.GetEdgeType(leftCell);
        var rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slope)
        {
            if (rightEdgeType == HexEdgeType.Slope)
            {
                TriangulateCornerTerraces(
                    bottom, bottomCell,
                    left, leftCell,
                    right, rightCell);
            }
            else if (rightEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(
                    left, leftCell,
                    right, rightCell,
                    bottom, bottomCell);
            }
            else
            {
                TriangulateCornerTerracesCliff(
                    bottom, bottomCell,
                    left, leftCell,
                    right, rightCell);
            }
        }
        else if (rightEdgeType == HexEdgeType.Slope)
        {
            if (leftEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(
                    right, rightCell,
                    bottom, bottomCell,
                    left, leftCell);
            }
            else
            {
                TriangulateCornerCliffTerraces(
                bottom, bottomCell,
                left, leftCell,
                right, rightCell);
            }
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            if (leftCell.Elevation < rightCell.Elevation)
            {
                TriangulateCornerCliffTerraces(
                    right, rightCell,
                    bottom, bottomCell,
                    left, leftCell);
            }
            else
            {
                TriangulateCornerTerracesCliff(
                    left, leftCell,
                    right, rightCell,
                    bottom, bottomCell);
            }
        }
        else
        {
            AddTriangle(
                bottom, left, right,
                bottomCell.Color, leftCell.Color, rightCell.Color);
        }
    }

    private void TriangulateCornerTerracesCliff(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        var b = Math.Abs(1f / (rightCell.Elevation - beginCell.Elevation));

        var boundary = Perturb(begin).Lerp(Perturb(right), b);
        var boundaryColor = beginCell.Color.Lerp(rightCell.Color, b);

        TriangulateBoundaryTriangle(
            begin, beginCell,
            left, leftCell,
            boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, leftCell,
                right, rightCell,
                boundary, boundaryColor
            );
        }
        else
        {
            AddTriangleUnperturbed(
                Perturb(left), Perturb(right), boundary,
                leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void TriangulateCornerCliffTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        var b = Math.Abs(1f / (leftCell.Elevation - beginCell.Elevation));
        var boundary = Perturb(begin).Lerp(Perturb(left), b);
        var boundaryColor = beginCell.Color.Lerp(leftCell.Color, b);

        TriangulateBoundaryTriangle(
            right, rightCell,
            begin, beginCell,
            boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, leftCell,
                right, rightCell,
                boundary, boundaryColor
            );
        }
        else
        {
            AddTriangle(
                Perturb(left), Perturb(right), boundary,
                leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void TriangulateBoundaryTriangle(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor)
    {
        var v1 = begin;
        var c1 = beginCell.Color;

        for (var i = 1; i <= HexMetrics.TerraceSteps; i++)
        {
            var v2 = HexMetrics.TerraceLerp(begin, left, i);
            var c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            AddTriangleUnperturbed(
                Perturb(v1), Perturb(v2), boundary,
                c1, c2, boundaryColor);

            v1 = v2;
            c1 = c2;
        }
    }

    private void TriangulateCornerTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    )
    {
        var v1 = HexMetrics.TerraceLerp(begin, left, 1);
        var v2 = HexMetrics.TerraceLerp(begin, right, 1);
        var c1 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
        var c2 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        AddTriangle(begin, v1, v2, beginCell.Color, c1, c2);

        for (var i = 2; i <= HexMetrics.TerraceSteps; i++)
        {
            var v3 = HexMetrics.TerraceLerp(begin, left, i);
            var v4 = HexMetrics.TerraceLerp(begin, right, i);
            var c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            var c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
            AddQuad(v1, v2, v3, v4, c1, c2, c3, c4);

            v1 = v3;
            v2 = v4;
            c1 = c3;
            c2 = c4;
        }
    }

    private void TriangulateEdgeTerraces(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell)
    {
        var e1 = begin;
        var c1 = beginCell.Color;

        for (var i = 1; i <= HexMetrics.TerraceSteps; i++)
        {
            var e2 = EdgeVertices.TerraceLerp(begin, end, i);
            var c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);

            TriangulateEdgeStrip(e1, c1, e2, c2);

            e1 = e2;
            c1 = c2;
        }
    }

    private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
                         Color c1, Color c2)
        => AddQuad(v1, v2, v3, v4, c1, c1, c2, c2);

    private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
                         Color c1, Color c2, Color c3, Color c4)
    {
        surfaceTool.AddTriangleFan(
        [
                Perturb(v1),
                Perturb(v2),
                Perturb(v4),
                Perturb(v3)
            ],
            colors: [c1, c2, c4, c3]);
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color c)
        => AddTriangle(v1, v2, v3, c, c, c);

    private void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Color c1, Color c2, Color c3)
    {
        surfaceTool.AddTriangleFan(
            [v1, v3, v2],
            colors: [c1, c3, c2]);
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color c1, Color c2, Color c3)
        => AddTriangleUnperturbed(
            Perturb(v1),
            Perturb(v2),
            Perturb(v3),
            c1,
            c2,
            c3);

    private static Vector3 Perturb(Vector3 position)
    {
        var sample = HexMetrics.SampleNoise(position);
        position.X += sample.X * HexMetrics.CellPerturbStrength;
        //position.Y += sample.Y * HexMetrics.CellPerturbStrength;
        position.Z += sample.Z * HexMetrics.CellPerturbStrength;
        return position;
    }

    protected override void Dispose(bool disposing)
    {
        surfaceTool.Dispose();
        base.Dispose(disposing);
    }
}
