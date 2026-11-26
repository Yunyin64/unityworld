using System.Collections.Generic;
using Godot;

namespace Xianxia.Client.Exploration;

/// <summary>样板用的原创山门和山亭。台基、柱梁和瓦坡都有实际几何体。</summary>
internal static class ExplorationArchitecture3D
{
    private const string SurfaceTextures = "res://assets/art/exploration/terrain3d/";

    internal static Node3D Build(bool gate)
    {
        var root = new Node3D { Name = gate ? "MountainGate" : "Pavilion" };
        var masonry = new List<Transform3D>();
        var timber = new List<Transform3D>();
        var trim = new List<Transform3D>();
        var tiles = new List<Transform3D>();
        float x = gate ? .94f : .29f;
        float z = gate ? .41f : .23f;
        float platform = gate ? .17f : .075f;
        float beam = gate ? 1.13f : .52f;

        Box(masonry, new(0, -.052f, 0), new(x * 2.39f, .25f, z * 2.67f));
        Box(masonry, new(0, platform - .032f, 0), new(x * 2.36f, .064f, z * 2.67f));
        int stepCount = gate ? 10 : 3;
        for (int i = 0; i < stepCount; i++)
        {
            float run = i * (gate ? .088f : .05f);
            float rise = platform - i * (gate ? .040f : .033f);
            Box(masonry, new(0, rise - .044f, z * 1.27f + run),
                new(x * (gate ? 1.38f : 1.28f), .088f, gate ? .113f : .08f));
            if (gate)
                Box(masonry, new(0, rise + .002f, z * 1.27f + run + .052f), new(x * 1.39f, .015f, .023f));
        }

        foreach (float side in new[] { -1f, 1f })
        {
            foreach (float depth in new[] { -z, z })
            {
                Column(side * x, depth);
                if (gate) Column(side * x * .43f, depth);
            }
            Box(timber, new(side * x, beam - .025f, 0), new(.118f, .13f, z * 2.33f));
            if (gate)
            {
                Box(masonry, new(side * x * 1.03f, .18f, z * 2.0f), new(.14f, .48f, .55f));
                Box(trim, new(side * x * 1.03f, .438f, z * 2.0f), new(.19f, .055f, .58f));
            }
        }

        foreach (float depth in new[] { -z, z })
        {
            Box(timber, new(0, beam - .044f, depth), new(x * 2.25f, gate ? .21f : .09f, gate ? .125f : .075f));
            Box(trim, new(0, beam + .076f, depth), new(x * 2.24f, .028f, .105f));
            int bays = gate ? 22 : 8;
            for (int i = 0; i < bays; i++)
            {
                float column = -x + i * (x * 2f / (bays - 1));
                Box(timber, new(column, beam + .095f, depth), new(.039f, .072f, .155f));
                Box(trim, new(column, beam + .115f, depth), new(.066f, .020f, .178f));
            }
        }

        AddRoof(root, tiles, beam + .126f, x * 1.34f, z * 1.57f, gate ? .41f : .225f);
        if (gate)
        {
            Box(timber, new(0, beam + .47f, 0), new(1.53f, .17f, .32f));
            for (int i = -5; i <= 5; i++)
                Box(trim, new(i * .125f, beam + .49f, .17f), new(.025f, .10f, .025f));
            AddRoof(root, tiles, beam + .54f, x * 1.04f, z * 1.17f, .34f);
            Box(timber, new(0, beam - .11f, z + .083f), new(.46f, .185f, .045f));
            Box(trim, new(0, beam - .024f, z + .111f), new(.44f, .021f, .027f));
            Box(trim, new(0, beam - .191f, z + .111f), new(.44f, .021f, .027f));
            AddLantern(root, trim, -.60f, beam - .29f, z + .04f);
            AddLantern(root, trim, .60f, beam - .29f, z + .04f);
        }
        Batch(root, "Masonry", masonry, ExplorationTerrainProps.Stone());
        Batch(root, "ColumnsAndBrackets", timber, TiledMaterial("wood", "#a95c46", .64f, new Vector3(2, .82f, 2), .43f));
        Batch(root, "Moulding", trim, TiledMaterial("wood", "#bd9765", .71f, new Vector3(2.7f, 1.6f, 2.7f), .32f));
        Batch(root, "RidgeTiles", tiles, TiledMaterial("roof", "#77838a", .7f, Vector3.One * 1.8f, .33f));
        return root;

        void Column(float px, float pz)
        {
            float size = gate ? .112f : .065f;
            Box(masonry, new(px, platform + .036f, pz), new(size * 1.5f, .072f, size * 1.5f));
            Box(timber, new(px, (platform + beam) * .5f, pz), new(size, beam - platform - .028f, size));
            Box(trim, new(px, beam - .105f, pz), new(size * 1.36f, .072f, size * 1.35f));
        }
    }

    private static void AddRoof(Node3D root, List<Transform3D> ridges, float eave, float x, float z, float rise)
    {
        var points = new List<Vector3>();
        var normals = new List<Vector3>();
        var colors = new List<Color>();
        var dark = new Color("#6f7b81").SrgbToLinear();
        var light = new Color("#a7b4b4").SrgbToLinear();
        Vector3 Profile(int face, float u, float t)
        {
            float bx = Mathf.Lerp(x, x * .61f, t);
            float bz = Mathf.Lerp(z, .006f, t);
            float uplift = .135f * Mathf.Pow(Mathf.Abs(u), 4f) * Mathf.Pow(1f - t, 3f);
            float y = eave + rise * (.16f * t + .84f * Mathf.Pow(t, 1.62f)) + uplift;
            return face switch
            {
                0 => new(u * bx, y, bz),
                1 => new(bx, y, -u * bz),
                2 => new(-u * bx, y, -bz),
                _ => new(-bx, y, u * bz)
            };
        }
        const int rows = 9;
        for (int face = 0; face < 4; face++)
        {
            int columns = Mathf.CeilToInt((face % 2 == 0 ? x : z) * 36f);
            for (int column = 0; column < columns; column++)
            {
                float u = 2f * column / columns - 1f;
                float v = 2f * (column + 1) / columns - 1f;
                for (int row = 0; row < rows; row++)
                {
                    float t = (float)row / rows;
                    float next = (float)(row + 1) / rows;
                    var a = Profile(face, u, t);
                    var b = Profile(face, v, t);
                    var c = Profile(face, u, next);
                    var d = Profile(face, v, next);
                    var color = dark.Lerp(light, .19f + .14f * Mathf.Sin(column * 2.45f + row * .9f));
                    RoofFace(a, b, c, color);
                    RoofFace(b, d, c, color);
                    var e = Profile(face, (u + v) * .5f, t) + Vector3.Up * .016f;
                    var f = Profile(face, (u + v) * .5f, next) + Vector3.Up * .016f;
                    Along(ridges, e, f, .021f);
                    if (row == 0) Along(ridges, a - Vector3.Up * .018f, b - Vector3.Up * .018f, .046f);
                }
            }
        }
        Along(ridges, new(-x * .65f, eave + rise + .039f, 0), new(x * .65f, eave + rise + .039f, 0), .063f);
        var shingle = TiledMaterial("roof", "#9daeb0", .76f, Vector3.One * 1.45f, .42f);
        shingle.VertexColorUseAsAlbedo = true;
        root.AddChild(new MeshInstance3D
        {
            Mesh = ExplorationTerrainMesh.Mesh(points, normals, colors),
            MaterialOverride = shingle
        });

        void RoofFace(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            var n = (c - a).Cross(b - a).Normalized();
            if (n.Y < 0)
            {
                (b, c) = (c, b);
                n = -n;
            }
            points.Add(a); points.Add(b); points.Add(c);
            normals.Add(n); normals.Add(n); normals.Add(n);
            colors.Add(color); colors.Add(color); colors.Add(color);
        }
    }

    private static void AddLantern(Node3D root, List<Transform3D> fittings, float x, float height, float z)
    {
        var material = Paint("#f39850", .74f);
        material.EmissionEnabled = true;
        material.Emission = new Color("#ff6026");
        material.EmissionEnergyMultiplier = 1.26f;
        root.AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { Height = .16f, TopRadius = .055f, BottomRadius = .039f, RadialSegments = 8 },
            MaterialOverride = material,
            Position = new Vector3(x, height, z)
        });
        Box(fittings, new(x, height + .084f, z), new(.117f, .013f, .117f));
        Box(fittings, new(x, height - .083f, z), new(.088f, .016f, .088f));
        Box(fittings, new(x, height + .147f, z), new(.013f, .126f, .012f));
        for (int i = 0; i < 4; i++)
        {
            var offset = new Vector3(Mathf.Cos(Mathf.Pi * .25f + i * Mathf.Pi * .5f), 0, Mathf.Sin(Mathf.Pi * .25f + i * Mathf.Pi * .5f));
            var position = new Vector3(x, height, z);
            Along(fittings, position + offset * .055f + Vector3.Up * .083f,
                position + offset * .041f - Vector3.Up * .083f, .005f);
        }
    }

    private static StandardMaterial3D Paint(string color, float roughness) =>
        new() { AlbedoColor = new Color(color), Roughness = roughness };

    private static StandardMaterial3D TiledMaterial(string texture, string tint, float roughness,
        Vector3 tiling, float relief)
    {
        var material = Paint(tint, roughness);
        material.AlbedoTexture = GD.Load<Texture2D>(SurfaceTextures + texture + "_color.webp");
        material.NormalTexture = GD.Load<Texture2D>(SurfaceTextures + texture + "_normal.webp");
        material.NormalEnabled = true;
        material.NormalScale = relief;
        material.Uv1Triplanar = true;
        material.Uv1WorldTriplanar = true;
        material.Uv1Scale = tiling;
        return material;
    }

    private static void Box(List<Transform3D> boxes, Vector3 p, Vector3 size) =>
        boxes.Add(new Transform3D(Basis.Identity.Scaled(size), p));

    private static void Along(List<Transform3D> rails, Vector3 start, Vector3 end, float width)
    {
        var delta = end - start;
        if (delta.LengthSquared() < .0000001f) return;
        var axis = delta.Normalized();
        var frame = Basis.LookingAt(axis, Mathf.Abs(axis.Y) > .97f ? Vector3.Right : Vector3.Up);
        var sizing = new Basis(Vector3.Right * width, Vector3.Up * width, Vector3.Back * delta.Length());
        rails.Add(new Transform3D(frame * sizing, (start + end) * .5f));
    }

    private static void Batch(Node3D root, string name, List<Transform3D> transforms, Material material)
    {
        if (transforms.Count == 0) return;
        var instances = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new BoxMesh { Size = Vector3.One }, InstanceCount = transforms.Count
        };
        for (int i = 0; i < transforms.Count; i++) instances.SetInstanceTransform(i, transforms[i]);
        root.AddChild(new MultiMeshInstance3D { Name = name, Multimesh = instances, MaterialOverride = material });
    }
}
