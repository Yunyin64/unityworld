using System.Collections.Generic;
using Godot;
using UnityWorld.Core;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.Exploration;

/// <summary>树木、岩石和草丛使用共享的立体模型。格心和邻域共同控制布点密度。</summary>
internal static class ExplorationTerrainProps
{
    private const string TerrainAssets = "res://assets/art/exploration/terrain3d/";
    private const string FoliageShader = "res://assets/presentation/exploration_foliage_3d.gdshader";

    internal static Node3D Build(ExplorationTerrainSource source, ExplorationTerrainSurface surface)
    {
        var props = new Node3D { Name = "Scenery" };
        var firs = new List<Prop>();
        var trees = new List<Prop>();
        var rocks = new List<Prop>();
        var grass = new List<Prop>();
        var flowers = new List<Prop>();
        foreach (var id in source.Visible)
        {
            var cell = source.Cells[id];
            if (cell.StillWater) continue;
            var center = ExplorationTerrainSurface.Center(id);
            var rng = new Rng(unchecked(source.Seed ^ (id.Q * 73856093) ^ (id.R * 19349663)));
            for (int i = 0; i < 152; i++)
            {
                var p = center + new Vector2(rng.Range(-.99f, .99f), rng.Range(-.86f, .86f));
                if (ExplorationTerrainSurface.CellAt(p) != id) continue;
                var sample = surface.Sample(p);
                if (source.Gate is TileId gate)
                {
                    var local = p - ExplorationTerrainSurface.Center(gate);
                    if (new Vector2(local.X * .78f, local.Y * .88f).LengthSquared() < .95f)
                        continue;
                }
                if (sample.Depth > -.046f || sample.Ground < .045f || sample.Trail > .34f ||
                    (cell.Landmark.Length > 0 && p.DistanceSquaredTo(center) < .29f)) continue;
                float scale = rng.Range(.62f, 1.14f);
                float rotation = rng.Range(0f, Mathf.Tau);
                float tint = rng.Range(.78f, 1.18f);
                var at = ExplorationTerrainSurface.At(p, sample.Ground - .018f);
                if (i < 44)
                {
                    float woodland = Mathf.Pow(sample.Trees, 1.6f) * (1f - sample.Mountain * .63f);
                    if (rng.Range(0f, 1f) > woodland * .96f || sample.Snow > .69f ||
                        surface.Normal(p).Y < .81f) continue;
                    (rng.Range(0f, 1f) > .27f ? firs : trees).Add(new(at, scale * rng.Range(.56f, 1.04f), rotation, tint));
                }
                else if (i < 56)
                {
                    float slope = 1f - surface.Normal(p).Y;
                    if (slope < .33f && rng.Range(0f, 1f) < .17f + slope)
                        rocks.Add(new(at - Vector3.Up * .026f, scale * rng.Range(.50f, 1.13f), rotation, tint));
                }
                else if (sample.Snow < .49f && sample.Mountain < .63f)
                {
                    grass.Add(new(at, scale, rotation, tint));
                    if (i % 9 == 0 && sample.Trees < .72f) flowers.Add(new(at, scale, rotation, tint));
                }
            }

            if (cell.Landmark.Length > 0)
            {
                var height = surface.Sample(center).Ground;
                if (source.Gate == id || source.Trail != null)
                {
                    var structure = ExplorationArchitecture3D.Build(source.Gate == id);
                    structure.Position = ExplorationTerrainSurface.At(center, height);
                    props.AddChild(structure);
                }
                else props.AddChild(new MeshInstance3D
                {
                    Name = "Landmark",
                    Mesh = LandmarkStone(),
                    MaterialOverride = PropMaterial(),
                    Position = ExplorationTerrainSurface.At(center, height - .07f)
                });
            }
        }
        var fir = ExplorationBotany.Fir();
        var broadleaf = ExplorationBotany.Broadleaf();
        AddBatch(props, "FirTrunks", fir.Wood, firs);
        AddBatch(props, "FirBoughs", fir.Foliage, firs, material: Leaves("fir_bough.webp", .028f, .29f));
        AddBatch(props, "BroadleafTrunks", broadleaf.Wood, trees);
        AddBatch(props, "BroadleafCanopies", broadleaf.Foliage, trees, material: Leaves("forest_leaf.webp", .049f, .34f));
        AddBatch(props, "Stones", Rock(), rocks, material: Stone(), outline: OutlineProfile.Stone);
        AddBatch(props, "Grass", Grass(false), grass, false, outline: OutlineProfile.Off);
        AddBatch(props, "Flowers", Grass(true), flowers, false, outline: OutlineProfile.Off);
        return props;
    }

    private static void AddBatch(Node3D parent, string name, ArrayMesh mesh, List<Prop> props, bool shadow = true,
        Material? material = null, OutlineProfile outline = OutlineProfile.Foliage)
    {
        if (props.Count == 0) return;
        var batch = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = mesh,
            InstanceCount = props.Count
        };
        for (int i = 0; i < props.Count; i++)
        {
            var prop = props[i];
            var basis = new Basis(Vector3.Up, prop.Angle).Scaled(Vector3.One * prop.Scale);
            batch.SetInstanceTransform(i, new Transform3D(basis, prop.Position));
            batch.SetInstanceColor(i, new Color(prop.Tint, prop.Tint, prop.Tint, 1));
        }
        var instance = new MultiMeshInstance3D
        {
            Name = name, Multimesh = batch, MaterialOverride = material ?? PropMaterial(),
            CastShadow = shadow ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off
        };
        ExplorationOutline3D.Mark(instance, outline);
        parent.AddChild(instance);
    }

    private static StandardMaterial3D PropMaterial() => new()
    {
        VertexColorUseAsAlbedo = true, Roughness = .89f, CullMode = BaseMaterial3D.CullModeEnum.Disabled
    };

    private static ShaderMaterial Leaves(string filename, float wind, float alphaCut)
    {
        var material = new ShaderMaterial { Shader = GD.Load<Shader>(FoliageShader) };
        material.SetShaderParameter("foliage", GD.Load<Texture2D>(TerrainAssets + filename));
        material.SetShaderParameter("wind_strength", wind);
        material.SetShaderParameter("alpha_cut", alphaCut);
        return material;
    }

    internal static StandardMaterial3D Stone()
    {
        return new StandardMaterial3D
        {
            AlbedoTexture = GD.Load<Texture2D>(TerrainAssets + "rock_color.webp"),
            NormalEnabled = true,
            NormalTexture = GD.Load<Texture2D>(TerrainAssets + "rock_normal.webp"),
            Uv1Triplanar = true,
            Uv1Scale = Vector3.One * 3.2f,
            Roughness = .85f
        };
    }

    private static ArrayMesh Rock()
    {
        var shape = new PropMesh();
        shape.Tier(Vector2.Zero, -.055f, .075f, .12f, .173f, 7, "#727d72", .72f, .028f);
        shape.Tier(Vector2.Zero, .075f, .144f, .173f, .037f, 7, "#8a9081", .72f, .028f);
        shape.Tier(Vector2.Zero, .144f, .156f, .037f, .001f, 7, "#98a186", .72f);
        return shape.Build();
    }

    private static ArrayMesh Grass(bool blossom)
    {
        var shape = new PropMesh();
        var green = new Color(blossom ? "#819a58" : "#647d3a").SrgbToLinear();
        var tips = new Color(blossom ? "#d5c6e2" : "#90a761").SrgbToLinear();
        var rng = new Rng(blossom ? 95 : 16);
        for (int i = 0; i < 15; i++)
        {
            float angle = rng.Range(0f, Mathf.Tau);
            var spread = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var width = Vector3.Up.Cross(spread) * rng.Range(.0045f, .0105f);
            var baseAt = spread * rng.Range(.008f, .045f);
            float reach = rng.Range(.046f, .121f);
            float height = rng.Range(.058f, .131f);
            var left = baseAt - width;
            var right = baseAt + width;
            for (int run = 1; run <= 3; run++)
            {
                float s = run / 3f;
                var blade = baseAt + spread * reach * s * s + Vector3.Up * height * s * (1.65f - .65f * s);
                var thickness = width * (1f - s);
                var nextLeft = blade - thickness;
                var nextRight = blade + thickness;
                var color = green.Lerp(tips, s * (blossom ? .45f : .26f));
                shape.Triangle(left, right, nextLeft, color);
                if (run < 3) shape.Triangle(nextLeft, right, nextRight, color);
                left = nextLeft;
                right = nextRight;
            }
        }
        if (blossom)
        {
            shape.Tier(new(.018f, -.032f), .10f, .14f, .012f, .035f, 6, "#dbbded");
            shape.Tier(new(.018f, -.032f), .14f, .149f, .035f, .006f, 6, "#f4dfa3");
        }
        return shape.Build();
    }

    private static ArrayMesh LandmarkStone()
    {
        var shape = new PropMesh();
        const float face = Mathf.Pi * .25f;
        shape.Tier(Vector2.Zero, -.09f, .12f, .20f, .17f, 4, "#535f60", face);
        shape.Tier(Vector2.Zero, .12f, .145f, .17f, .10f, 4, "#867e67", face);
        shape.Tier(Vector2.Zero, .145f, .52f, .09f, .073f, 4, "#9c966f", face, rectangular: true);
        shape.Tier(Vector2.Zero, .52f, .57f, .073f, .01f, 4, "#bfaa71", face, rectangular: true);
        return shape.Build();
    }

    private readonly record struct Prop(Vector3 Position, float Scale, float Angle, float Tint);

    private sealed class PropMesh
    {
        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector3> _normals = new();
        private readonly List<Color> _colors = new();

        internal ArrayMesh Build() => ExplorationTerrainMesh.Mesh(_vertices, _normals, _colors);

        internal void Tier(Vector2 center, float low, float high, float lowerRadius, float upperRadius, int count,
            string html, float phase = 0f, float jag = 0f, bool rectangular = false)
        {
            var color = new Color(html).SrgbToLinear();
            for (int i = 0; i < count; i++)
            {
                var a = Ring(i, low, lowerRadius);
                var b = Ring(i + 1, low, lowerRadius);
                var c = Ring(i, high, upperRadius);
                var d = Ring(i + 1, high, upperRadius);
                float shade = .83f + .17f * Mathf.Sin((i + 1) * 2.7f);
                Triangle(a, b, c, color * shade);
                Triangle(b, d, c, color * (shade + .095f));
            }

            Vector3 Ring(int index, float y, float radius)
            {
                int around = index % count;
                float angle = around * Mathf.Tau / count + phase;
                float spread = radius * (1f + (rectangular ? 0f : .12f * Mathf.Sin(around * 2.31f)));
                float zScale = rectangular ? .74f : 1f;
                return new(center.X + spread * Mathf.Cos(angle), y + jag * Mathf.Cos(around * 2.13f),
                    center.Y + spread * Mathf.Sin(angle) * zScale);
            }
        }

        internal void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            var normal = (c - a).Cross(b - a).Normalized();
            _vertices.Add(a); _vertices.Add(b); _vertices.Add(c);
            _normals.Add(normal); _normals.Add(normal); _normals.Add(normal);
            _colors.Add(color); _colors.Add(color); _colors.Add(color);
        }
    }
}
