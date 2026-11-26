using System.Collections.Generic;
using Godot;
using UnityWorld.Core;

namespace Xianxia.Client.Exploration;

/// <summary>用真实枝叶纹理沿三维枝干排布叶片，视角转动时能看到完整树冠。</summary>
internal static class ExplorationBotany
{
    internal static (ArrayMesh Wood, ArrayMesh Foliage) Fir()
    {
        var bark = new TreeMesh();
        var foliage = new TreeMesh();
        var rng = new Rng(6821);
        var trunk = new Color("#57473b").SrgbToLinear();
        Vector3 Ridge(float y) => new(.09f * y * y, y, -.045f * y);
        for (int ring = 0; ring < 7; ring++)
        {
            float y = ring / 7f * 1.29f;
            float radius = .064f * (1f - ring / 7.4f);
            bark.Stem(Ridge(y), Ridge(y + 1.29f / 7f), radius, radius * .82f, trunk);
        }
        for (int ring = 0; ring < 8; ring++)
        {
            float band = ring / 7f;
            int count = ring < 5 ? 5 : 4;
            for (int branch = 0; branch < count; branch++)
            {
                float theta = ring * 1.75f + branch * Mathf.Tau / count + rng.Range(-.25f, .25f);
                var heading = new Vector3(Mathf.Cos(theta), 0, Mathf.Sin(theta));
                var origin = Ridge(.30f + band * .86f + rng.Range(-.044f, .041f));
                float width = Mathf.Lerp(.54f, .139f, band) * rng.Range(.76f, 1.16f);
                var knot = origin + heading * (width * .52f) + Vector3.Down * (.10f * (1f - band));
                var tip = origin + heading * width + Vector3.Up * rng.Range(-.035f, .09f);
                bark.Stem(origin, knot, .014f * (1f - band * .61f), .009f, trunk);
                bark.Stem(knot, tip, .009f, .002f, trunk);
                var across = Vector3.Up.Cross(heading);
                for (int sprig = 1; sprig < 4; sprig++)
                {
                    float t = sprig / 4f;
                    var joint = knot.Lerp(tip, t);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        var direction = (heading * .47f + across * side * .68f).Normalized();
                        float length = width * rng.Range(.37f, .55f) * (1f - t * .43f);
                        var end = joint + direction * length + Vector3.Up * rng.Range(.025f, .11f);
                        float shade = rng.Range(.69f, 1.16f);
                        foliage.Leaf(joint, end, length * .46f, shade, side * rng.Range(-.45f, .45f));
                        foliage.Leaf(joint, end + Vector3.Down * .035f, length * .34f, shade * .83f, side * .91f);
                    }
                }
                foliage.Leaf(knot, tip + heading * (.15f * width) + Vector3.Up * .105f, width * .27f, .9f, -.15f);
            }
        }
        foreach (float offset in new[] { -.07f, .07f })
            foliage.Leaf(Ridge(1.12f), Ridge(1.40f) + new Vector3(offset, 0, offset), .105f, 1.14f, offset * 12f);
        return (bark.Build(), foliage.Build());
    }

    internal static (ArrayMesh Wood, ArrayMesh Foliage) Broadleaf()
    {
        var bark = new TreeMesh();
        var foliage = new TreeMesh();
        var rng = new Rng(1325);
        var trunk = new Color("#635245").SrgbToLinear();
        var middle = new Vector3(-.064f, .39f, .031f);
        var top = new Vector3(.085f, .79f, -.03f);
        bark.Stem(Vector3.Down * .04f, middle, .069f, .043f, trunk);
        bark.Stem(middle, top, .043f, .019f, trunk);
        for (int branch = 0; branch < 11; branch++)
        {
            float phase = branch * 2.399963f;
            var heading = new Vector3(Mathf.Sin(phase), 0f, Mathf.Cos(phase));
            var start = middle.Lerp(top, branch / 13f);
            var joint = start + heading * rng.Range(.17f, .31f) + Vector3.Up * rng.Range(.08f, .18f);
            bark.Stem(start, joint, .019f, .011f, trunk);
            for (int twig = 0; twig < 5; twig++)
            {
                var direction = heading.Rotated(Vector3.Up, (twig - 2) * .61f);
                var end = joint + direction * rng.Range(.10f, .22f) + Vector3.Up * rng.Range(.026f, .159f);
                bark.Stem(joint, end, .010f, .003f, trunk);
                for (int pair = 0; pair < 5; pair++)
                {
                    var attachment = joint.Lerp(end, .26f + .17f * pair);
                    var leaf = direction.Rotated(Vector3.Up, pair % 2 == 0 ? 1.04f : -1.04f);
                    float length = rng.Range(.095f, .177f);
                    foliage.Leaf(attachment, attachment + leaf * length + Vector3.Up * rng.Range(.01f, .074f),
                        length * .29f, rng.Range(.77f, 1.21f), rng.Range(-.64f, .64f));
                }
            }
        }
        return (bark.Build(), foliage.Build());
    }

    private sealed class TreeMesh
    {
        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector3> _normals = new();
        private readonly List<Color> _colors = new();
        private readonly List<Vector2> _uv = new();

        internal ArrayMesh Build() => ExplorationTerrainMesh.Mesh(_vertices, _normals, _colors, _uv);

        internal void Stem(Vector3 from, Vector3 to, float baseRadius, float endRadius, Color color)
        {
            var axis = (to - from).Normalized();
            var right = axis.Cross(Mathf.Abs(axis.Y) > .88f ? Vector3.Forward : Vector3.Up).Normalized();
            for (int i = 0; i < 6; i++)
            {
                var ray = right.Rotated(axis, i * Mathf.Tau / 6f);
                var next = right.Rotated(axis, (i + 1) * Mathf.Tau / 6f);
                var a = from + ray * baseRadius;
                var b = from + next * baseRadius;
                var c = to + ray * endRadius;
                var d = to + next * endRadius;
                var smooth = (ray + next).Normalized();
                Face(a, c, b, smooth, smooth, smooth, color, Vector2.Zero, Vector2.Zero, Vector2.Zero);
                Face(b, c, d, smooth, smooth, smooth, color, Vector2.Zero, Vector2.Zero, Vector2.Zero);
            }
        }

        internal void Leaf(Vector3 start, Vector3 tip, float halfWidth, float tint, float roll)
        {
            var forward = (tip - start).Normalized();
            var right = forward.Cross(Mathf.Abs(forward.Y) > .95f ? Vector3.Right : Vector3.Up).Normalized().Rotated(forward, roll);
            var leafNormal = right.Cross(forward);
            if (leafNormal.Y < 0f) leafNormal = -leafNormal;
            var normal = (leafNormal + Vector3.Up * .36f).Normalized();
            var middle = start.Lerp(tip, .50f) + normal * (halfWidth * .24f);
            var a = start - right * halfWidth;
            var b = start + right * halfWidth;
            var c = tip - right * halfWidth;
            var d = tip + right * halfWidth;
            var color = new Color(tint, tint, tint, 1f);
            var center = new Vector2(.5f, .5f);
            Face(a, c, middle, normal, normal, normal, color, new(0f, 1f), new(0f, 0f), center);
            Face(c, d, middle, normal, normal, normal, color, new(0f, 0f), new(1f, 0f), center);
            Face(d, b, middle, normal, normal, normal, color, new(1f, 0f), new(1f, 1f), center);
            Face(b, a, middle, normal, normal, normal, color, new(1f, 1f), new(0f, 1f), center);
        }

        private void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 n1, Vector3 n2, Vector3 n3, Color color,
            Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            _vertices.Add(a); _vertices.Add(b); _vertices.Add(c);
            _normals.Add(n1); _normals.Add(n2); _normals.Add(n3);
            _colors.Add(color); _colors.Add(color); _colors.Add(color);
            _uv.Add(uv1); _uv.Add(uv2); _uv.Add(uv3);
        }
    }
}
