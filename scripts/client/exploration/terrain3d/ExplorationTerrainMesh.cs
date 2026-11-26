using System;
using System.Collections.Generic;
using Godot;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.Exploration;

/// <summary>每格的三角面先按六边形生成，再批量提交给 Godot。选格使用实际显示的三角面。</summary>
internal sealed class ExplorationTerrainMesh
{
    private const int Steps = 16;
    private readonly ExplorationTerrainSource _source;
    private readonly ExplorationTerrainSurface _surface;
    private readonly Dictionary<Vector2I, int> _indexByPoint = new();
    private readonly List<Vector3> _landVertices = new();
    private readonly List<Vector3> _normals = new();
    private readonly List<Color> _colors = new();
    private readonly List<Vector2> _attributes = new();
    private readonly List<Vector2> _surfaceDetails = new();
    private readonly List<float> _depths = new();
    private readonly List<int> _landIndices = new();
    private readonly List<Vector3> _waterVertices = new();
    private readonly List<Vector2> _waterDepths = new();
    private readonly Dictionary<TileId, CellFaces> _faces = new();

    internal ExplorationTerrainMesh(ExplorationTerrainSource source, ExplorationTerrainSurface surface)
    {
        _source = source;
        _surface = surface;
        foreach (var id in source.Visible)
        {
            var lattice = ExplorationTerrainSurface.LatticeCenter(id) * Steps;
            int landStart = _landIndices.Count, waterStart = _waterVertices.Count;
            for (int edge = 0; edge < 6; edge++)
            {
                var first = ExplorationTerrainSurface.Corners[edge];
                var next = ExplorationTerrainSurface.Corners[(edge + 1) % 6];
                for (int i = 0; i < Steps; i++)
                for (int j = 0; j < Steps - i; j++)
                {
                    var corner = lattice + first * i + next * j;
                    int a = Vertex(corner), b = Vertex(corner + first), c = Vertex(corner + next);
                    AddTriangle(a, b, c);
                    if (i + j < Steps - 1) AddTriangle(b, Vertex(corner + first + next), c);
                }
            }
            var bounds = new Aabb(_landVertices[_landIndices[landStart]], Vector3.Zero);
            for (int i = landStart + 1; i < _landIndices.Count; i++) bounds = bounds.Expand(_landVertices[_landIndices[i]]);
            for (int i = waterStart; i < _waterVertices.Count; i++) bounds = bounds.Expand(_waterVertices[i]);
            _faces.Add(id, new(bounds.Grow(.001f), landStart, _landIndices.Count, waterStart, _waterVertices.Count));
        }
    }

    internal int LandTriangles => _landIndices.Count / 3;
    internal int WaterTriangles => _waterVertices.Count / 3;

    internal ArrayMesh LandMesh() => Mesh(_landVertices, _normals, _colors, _attributes, _landIndices, _surfaceDetails);
    internal ArrayMesh WaterMesh() => Mesh(_waterVertices, null, null, _waterDepths);

    internal TileId? Pick(Vector3 start, Vector3 direction, float range)
    {
        TileId? result = null;
        float closest = range;
        foreach (var (id, faces) in _faces)
        {
            if (!faces.Bounds.IntersectsSegment(start, start + direction * closest)) continue;
            for (int i = faces.LandStart; i < faces.LandEnd; i += 3)
                Check(_landVertices[_landIndices[i]], _landVertices[_landIndices[i + 1]], _landVertices[_landIndices[i + 2]]);
            for (int i = faces.WaterStart; i < faces.WaterEnd; i += 3)
                Check(_waterVertices[i], _waterVertices[i + 1], _waterVertices[i + 2]);

            void Check(Vector3 a, Vector3 b, Vector3 c)
            {
                var hit = Geometry3D.RayIntersectsTriangle(start, direction, a, b, c);
                if (hit.VariantType == Variant.Type.Nil) return;
                float distance = start.DistanceTo(hit.AsVector3());
                if (distance >= closest) return;
                result = id;
                closest = distance;
            }
        }
        return result;
    }

    internal ArrayMesh GridMesh(TileId? selection)
    {
        var vertices = new List<Vector3>();
        if (selection is TileId id && _source.Visible.Contains(id))
        {
            for (int edge = 0; edge < 6; edge++) AddEdge(vertices, id, edge, .035f);
        }
        else if (selection == null)
        {
            foreach (var cell in _source.Visible)
                for (int edge = 0; edge < 6; edge++)
                    if (edge < 3 || !_source.Visible.Contains(cell.Neighbor(edge)))
                        AddEdge(vertices, cell, edge, .017f);
        }
        return Mesh(vertices);
    }

    internal ArrayMesh RimMesh()
    {
        float baseHeight = float.PositiveInfinity;
        for (int i = 0; i < _landVertices.Count; i++)
            baseHeight = Mathf.Min(baseHeight, _landVertices[i].Y + Mathf.Max(_depths[i], 0f));
        baseHeight -= .52f;
        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var soil = new Color("#676451").SrgbToLinear();
        var stone = new Color("#464c49").SrgbToLinear();
        var baseColor = new Color("#242f32").SrgbToLinear();
        foreach (var id in _source.Visible)
        {
            for (int edge = 0; edge < 6; edge++)
            {
                if (_source.Visible.Contains(id.Neighbor(edge))) continue;
                var a = EdgePoint(id, edge, 0);
                for (int i = 1; i <= Steps; i++)
                {
                    var b = EdgePoint(id, edge, i);
                    var midA = a - Vector3.Up * .18f;
                    var midB = b - Vector3.Up * .18f;
                    AddBand(a, b, midA, midB, soil, stone);
                    AddBand(midA, midB, new Vector3(a.X, baseHeight, a.Z), new Vector3(b.X, baseHeight, b.Z), stone, baseColor);
                    a = b;
                }
            }
        }
        return Mesh(vertices, colors: colors);

        void AddBand(Vector3 a, Vector3 b, Vector3 lowA, Vector3 lowB, Color top, Color bottom)
        {
            vertices.Add(a); vertices.Add(lowA); vertices.Add(b);
            vertices.Add(b); vertices.Add(lowA); vertices.Add(lowB);
            colors.Add(top); colors.Add(bottom); colors.Add(top);
            colors.Add(top); colors.Add(bottom); colors.Add(bottom);
        }
    }

    private void AddEdge(List<Vector3> vertices, TileId id, int edge, float width)
    {
        var a = EdgePoint(id, edge, 0) + Vector3.Up * .021f;
        var b = EdgePoint(id, edge, Steps);
        var across = new Vector3(b.Z - a.Z, 0, a.X - b.X).Normalized() * (width * .5f);
        for (int i = 1; i <= Steps; i++)
        {
            b = EdgePoint(id, edge, i) + Vector3.Up * .021f;
            vertices.Add(a - across); vertices.Add(a + across); vertices.Add(b - across);
            vertices.Add(b - across); vertices.Add(a + across); vertices.Add(b + across);
            a = b;
        }
    }

    private Vector3 EdgePoint(TileId id, int edge, int t)
    {
        var p = ExplorationTerrainSurface.LatticeCenter(id) * Steps
            + ExplorationTerrainSurface.Corners[edge] * (Steps - t)
            + ExplorationTerrainSurface.Corners[(edge + 1) % 6] * t;
        int i = _indexByPoint[p];
        return _landVertices[i] + Vector3.Up * Mathf.Max(_depths[i], 0f);
    }

    private int Vertex(Vector2I lattice)
    {
        if (_indexByPoint.TryGetValue(lattice, out int index)) return index;
        index = _landVertices.Count;
        var p = ExplorationTerrainSurface.Position(lattice) / Steps;
        var sample = _surface.Sample(p);
        _indexByPoint.Add(lattice, index);
        _landVertices.Add(ExplorationTerrainSurface.At(p, sample.Ground));
        _normals.Add(_surface.Normal(p));
        _colors.Add(sample.Color);
        _attributes.Add(new(sample.Mountain, sample.Snow));
        _surfaceDetails.Add(new(sample.Trail, sample.Depth));
        _depths.Add(sample.Depth);
        return index;
    }

    private void AddTriangle(int a, int b, int c)
    {
        _landIndices.Add(a); _landIndices.Add(b); _landIndices.Add(c);
        if (_depths[a] <= 0 && _depths[b] <= 0 && _depths[c] <= 0) return;
        Span<WaterPoint> polygon = stackalloc WaterPoint[4];
        int count = 0;
        Clip(c, a, polygon, ref count);
        Clip(a, b, polygon, ref count);
        Clip(b, c, polygon, ref count);
        for (int i = 1; i + 1 < count; i++)
        {
            AddWater(polygon[0]);
            AddWater(polygon[i]);
            AddWater(polygon[i + 1]);
        }
    }

    private void Clip(int previous, int next, Span<WaterPoint> polygon, ref int count)
    {
        float oldDepth = _depths[previous], depth = _depths[next];
        if ((oldDepth > 0) != (depth > 0))
        {
            var bank = _landVertices[previous].Lerp(_landVertices[next], oldDepth / (oldDepth - depth));
            polygon[count++] = new(bank + Vector3.Up * .004f, 0);
        }
        if (depth > 0) polygon[count++] = new(_landVertices[next] + Vector3.Up * (depth + .004f), depth);
    }

    private void AddWater(WaterPoint point)
    {
        _waterVertices.Add(point.Position);
        _waterDepths.Add(new(point.Depth, 0));
    }

    internal static ArrayMesh Mesh(List<Vector3> vertices, List<Vector3>? normals = null, List<Color>? colors = null,
        List<Vector2>? uv = null, List<int>? indices = null, List<Vector2>? uv2 = null)
    {
        var mesh = new ArrayMesh();
        if (vertices.Count == 0) return mesh;
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Godot.Mesh.ArrayType.Max);
        arrays[(int)Godot.Mesh.ArrayType.Vertex] = vertices.ToArray();
        if (normals != null) arrays[(int)Godot.Mesh.ArrayType.Normal] = normals.ToArray();
        if (colors != null) arrays[(int)Godot.Mesh.ArrayType.Color] = colors.ToArray();
        if (uv != null) arrays[(int)Godot.Mesh.ArrayType.TexUV] = uv.ToArray();
        if (uv2 != null) arrays[(int)Godot.Mesh.ArrayType.TexUV2] = uv2.ToArray();
        if (indices != null) arrays[(int)Godot.Mesh.ArrayType.Index] = indices.ToArray();
        mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    private readonly record struct WaterPoint(Vector3 Position, float Depth);
    private readonly record struct CellFaces(Aabb Bounds, int LandStart, int LandEnd, int WaterStart, int WaterEnd);
}
