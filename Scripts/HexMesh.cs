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

        v3.Y = v4.Y = neighbor.Elevation * HexMetrics.ElevationStep;

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(v1, v2, cell, v3, v4, neighbor);
        }
        else
        {
            AddQuad(v1, v2, v3, v4,
                cell.Color, cell.Color, neighbor.Color, neighbor.Color);
        }

        var nextNeighbor = cell.GetNeighbor(direction.Next());

        if (direction <= HexDirection.E && nextNeighbor is not null)
        {
            var v5 = v2 + HexMetrics.GetBridge(direction.Next());
            v5.Y = nextNeighbor.Elevation * HexMetrics.ElevationStep;

            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(
                        v2, cell,
                        v4, neighbor,
                        v5, nextNeighbor);
                }
                else
                {
                    TriangulateCorner(v5, nextNeighbor,
                        v2, cell, v4, neighbor);
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(v4, neighbor, v5, nextNeighbor, v2, cell);
            }
            else
            {
                TriangulateCorner(v5, nextNeighbor, v2, cell, v4, neighbor);
            }
        }
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

        var boundary = begin.Lerp(right, b);
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
            AddTriangle(
                left, right, boundary,
                leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void TriangulateCornerCliffTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        var b = Math.Abs(1f / (leftCell.Elevation - beginCell.Elevation));
        var boundary = begin.Lerp(left, b);
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
                left, right, boundary,
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
            AddTriangle(v1, v2, boundary, c1, c2, boundaryColor);

            v1 = v2;
            c1 = c2;
        }

        AddTriangle(v1, left, boundary, c1, leftCell.Color, boundaryColor);
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

    private void TriangulateEdgeTerraces(Vector3 beginLeft, Vector3 beginRight, HexCell beginCell,
                                         Vector3 endLeft, Vector3 endRight, HexCell endCell)
    {
        var v1 = beginLeft;
        var v2 = beginRight;
        var c1 = beginCell.Color;

        for (var i = 1; i <= HexMetrics.TerraceSteps; i++)
        {
            var v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, i);
            var v4 = HexMetrics.TerraceLerp(beginRight, endRight, i);
            var c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);

            AddQuad(v1, v2, v3, v4,
                    c1, c1, c2, c2);

            v1 = v3;
            v2 = v4;
            c1 = c2;
        }
    }

    private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
                         Color c1, Color c2, Color c3, Color c4)
    {
        surfaceTool.AddTriangleFan(
            [v1, v2, v4, v3],
            colors: [c1, c2, c3, c4]);
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color c1, Color c2, Color c3)
    {
        surfaceTool.AddTriangleFan(
            [v1, v3, v2],
            colors: [c1, c3, c2]);
    }

    protected override void Dispose(bool disposing)
    {
        surfaceTool.Dispose();
        base.Dispose(disposing);
    }
}
