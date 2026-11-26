using System.Collections.Generic;
using Godot;

namespace Xianxia.Client.Exploration;

/// <summary>样板近景的远山，由实景照明与雾统一处理。远山在可交互地块之外。</summary>
internal static class ExplorationScenicBackdrop
{
    private static readonly Peak[] Peaks =
    {
        new(new(-17, -20), 4.9f, new(6.7f, 4.2f), .46f),
        new(new(-13, -19), 6.9f, new(5.7f, 3.9f), -.51f),
        new(new(-9.6f, -18), 4.9f, new(5.6f, 3.9f), -.77f),
        new(new(-6.2f, -21), 6.1f, new(7.3f, 5.1f), .58f),
        new(new(-1.9f, -23), 3.7f, new(5.6f, 4.5f), .63f),
        new(new(6.4f, -24), 6.6f, new(7.2f, 4.2f), -.38f),
        new(new(14, -25), 7.5f, new(8.5f, 5.1f), -.44f),
        new(new(-21, -34), 7.9f, new(9.2f, 5.5f), .37f),
        new(new(-13, -35), 10.8f, new(8.6f, 5.1f), .23f),
        new(new(-3.5f, -36), 9.4f, new(9.1f, 5.2f), -.37f),
        new(new(6, -34), 8.9f, new(9.9f, 5.6f), .34f),
        new(new(16, -37), 9.4f, new(9.3f, 5.5f), -.54f)
    };

    internal static MeshInstance3D Build(Material material)
    {
        const int xSteps = 263, zSteps = 157;
        const float step = .22f;
        const int row = xSteps + 1;
        var vertices = new List<Vector3>(row * (zSteps + 1));
        var normals = new List<Vector3>(vertices.Capacity);
        var colors = new List<Color>(vertices.Capacity);
        var uv = new List<Vector2>(vertices.Capacity);
        var uv2 = new List<Vector2>(vertices.Capacity);
        var indices = new List<int>(xSteps * zSteps * 6);
        var noise = new FastNoiseLite { Seed = 54328, Frequency = 1f, FractalType = FastNoiseLite.FractalTypeEnum.None };
        for (int iz = 0; iz <= zSteps; iz++)
        for (int ix = 0; ix <= xSteps; ix++)
        {
            var p = new Vector2(-29 + ix * step, -45 + iz * step);
            float h = Height(p);
            float left = Height(p - new Vector2(step, 0)), right = Height(p + new Vector2(step, 0));
            float back = Height(p - new Vector2(0, step)), front = Height(p + new Vector2(0, step));
            vertices.Add(new Vector3(p.X, h, p.Y));
            normals.Add(new Vector3(left - right, 2 * step, back - front).Normalized());
            float hue = .5f + noise.GetNoise2D(p.X * .35f, p.Y * .44f) * .5f;
            colors.Add(new Color("#404a41").SrgbToLinear().Lerp(new Color("#666e56").SrgbToLinear(), hue));
            uv.Add(new Vector2(.81f, Mathf.SmoothStep(4f, 12f, h) * .57f));
            uv2.Add(new Vector2(0, -.2f));
        }
        for (int z = 0; z < zSteps; z++)
        for (int x = 0; x < xSteps; x++)
        {
            int a = x + z * row, b = a + 1, c = a + row, d = c + 1;
            if (Mathf.Max(Mathf.Max(vertices[a].Y, vertices[b].Y), Mathf.Max(vertices[c].Y, vertices[d].Y)) < -.10f)
                continue;
            indices.Add(a); indices.Add(b); indices.Add(c);
            indices.Add(b); indices.Add(d); indices.Add(c);
        }
        return new MeshInstance3D
        {
            Name = "DistantRidges",
            Mesh = ExplorationTerrainMesh.Mesh(vertices, normals, colors, uv, indices, uv2),
            MaterialOverride = material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };

        float Height(Vector2 p)
        {
            float height = -1f;
            foreach (var peak in Peaks)
            {
                var d = (p - peak.Point).Rotated(peak.Angle) / peak.Radius;
                float r = d.Length();
                if (r >= 1.3f) continue;
                float around = d.Angle();
                float skirt = 1f + .16f * Mathf.Sin(5f * around + peak.Angle * 2.9f)
                    + .085f * Mathf.Cos(9f * around + peak.Point.X);
                float profile = Mathf.Pow(Mathf.Max(1f - r / skirt, 0f), 1.19f);
                height = Mathf.Max(height, -1f + peak.Height * profile);
            }
            float terrain = noise.GetNoise2D(p.X * .68f, p.Y * 1.31f) * .54f +
                noise.GetNoise2D(p.X * 2.9f, p.Y * 4.6f) * .18f;
            return height + terrain * Mathf.Clamp(height * .30f, 0f, 1f);
        }
    }

    private readonly record struct Peak(Vector2 Point, float Height, Vector2 Radius, float Angle);
}
