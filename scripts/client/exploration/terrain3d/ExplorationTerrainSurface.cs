using Godot;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.Exploration;

internal readonly record struct TerrainPoint3D(float Ground, float Depth, Color Color, float Mountain, float Snow, float Trees, float Trail)
{
    internal float Surface => Ground + Mathf.Max(Depth, 0f);
}

/// <summary>统一在世界坐标采样高度。两格共边和三格共角会读取同一组格心及权重。</summary>
internal sealed class ExplorationTerrainSurface(ExplorationTerrainSource source)
{
    internal const float HalfHeight = .8660254f;
    internal static readonly Vector2I[] Corners =
        { new(2, 0), new(1, 1), new(-1, 1), new(-2, 0), new(-1, -1), new(1, -1) };

    private readonly FastNoiseLite _noise = new()
    {
        Seed = source.Seed, Frequency = 1f, FractalType = FastNoiseLite.FractalTypeEnum.None
    };

    internal static Vector2I LatticeCenter(TileId id) => new(3 * id.Q, 2 * id.R + id.Q);
    internal static Vector2 Position(Vector2I p) => new(p.X * .5f, p.Y * HalfHeight);
    internal static Vector2 Center(TileId id) => Position(LatticeCenter(id));
    internal static Vector3 At(Vector2 p, float height) => new(p.X, height, p.Y);

    internal static TileId CellAt(Vector2 flat) =>
        ExplorationHexProjection.Unproject(new Vector2(flat.X, flat.Y * ExplorationHexProjection.Compression), 1f);

    internal TerrainPoint3D Sample(Vector2 p)
    {
        // 轴坐标平行四边形由两块格心三角形组成，用重心权重做连续插值。
        float q = p.X / 1.5f;
        float r = p.Y / (2f * HalfHeight) - q * .5f;
        int iq = Mathf.FloorToInt(q), ir = Mathf.FloorToInt(r);
        float u = q - iq, v = r - ir;
        bool upper = u + v > 1f;
        var a = new TileId(iq + (upper ? 1 : 0), ir + (upper ? 1 : 0));
        var b = new TileId(iq + 1, ir);
        var c = new TileId(iq, ir + 1);
        float wa = Mathf.Abs(1f - u - v), wb = upper ? 1f - v : u, wc = upper ? 1f - u : v;
        var blend = new TerrainBlend();
        float ma = Accumulate(ref blend, a, wa, p);
        float mb = Accumulate(ref blend, b, wb, p);
        float mc = Accumulate(ref blend, c, wc, p);
        if (blend.Total <= 0) return new(-.65f, -.5f, new Color("#586d62").SrgbToLinear(), 0, 0, 0, 0);

        float mountain = blend.Mountain / blend.Total;
        float ridges = PairRidge(wa, wb, ma, mb) + PairRidge(wb, wc, mb, mc) + PairRidge(wc, wa, mc, ma);
        float broad = Noise(p, 1.2f), detail = Noise(p, 7.4f);
        float folds = Noise(new Vector2(p.X * 3.6f + p.Y * .5f, p.Y * 1.46f), 1.0f);
        float peaks = blend.Peak + .075f * Noise(p, 2.8f) + .10f * folds + .022f * detail;
        float dryGround = blend.Land / Mathf.Max(blend.Dry, .001f);
        dryGround += .061f * broad + .009f * detail + mountain * (peaks + ridges);
        if (source.Gate is TileId gate)
        {
            // 山门附近用地基过渡到自然山坡，为台阶保留可读的前场。
            var local = p - Center(gate);
            float pad = new Vector2(local.X * .78f, local.Y * 1.05f).Length();
            float terrace = .85f - Mathf.Max(local.Y, 0f) * .24f;
            dryGround = Mathf.Lerp(terrace, dryGround, Mathf.SmoothStep(.68f, 1.17f, pad));
        }
        float ground = dryGround;
        float depth = -.25f;
        if (blend.Wet > .0001f)
        {
            float water = blend.Level / blend.Wet;
            float wetFraction = blend.Wet / blend.Total;
            float shoreline = .51f + .11f * Noise(p, 2.35f) + .038f * detail;
            if (wetFraction < shoreline)
            {
                float landSide = 1f - wetFraction / shoreline;
                float steepness = Mathf.Lerp(1.45f, .58f, Mathf.Clamp(mountain * 2.4f, 0, 1));
                ground = water + Mathf.Max(.17f, dryGround - water) * Mathf.Pow(landSide, steepness);
            }
            else
            {
                float wetSide = (wetFraction - shoreline) / (1f - shoreline);
                ground = Mathf.Lerp(water, blend.Bed / blend.Wet, Mathf.SmoothStep(0f, 1f, wetSide));
            }
            depth = water - ground;
        }

        if (blend.Channel > 0f)
        {
            float river = ground - .026f;
            ground -= blend.Channel * .26f;
            depth = Mathf.Max(depth + blend.Channel * .26f, river - ground);
        }

        var color = blend.Dry > .001f ? blend.Color / blend.Dry : new Color("#375c55").SrgbToLinear();
        color *= .84f + broad * .19f + detail * .09f;
        if (blend.Wet > .0001f && depth > -.14f)
        {
            var beach = new Color("#c4ad7e").SrgbToLinear();
            var rock = new Color("#77796b").SrgbToLinear();
            color = color.Lerp(beach.Lerp(rock, Mathf.Clamp(mountain * 2.5f, 0f, 1f)),
                Mathf.SmoothStep(0f, 1f, 1f + depth / .14f) * .91f);
        }
        float trail = depth < -.036f ? TrailAt(p, detail) : 0f;
        color = color.Lerp(new Color("#ad865b").SrgbToLinear(), trail * .89f);
        return new(ground, depth, color, mountain, blend.Snow / blend.Total, blend.Trees / blend.Total, trail);
    }

    internal Vector3 Normal(Vector2 p)
    {
        const float step = .025f;
        var dx = new Vector2(step, 0);
        var dz = new Vector2(0, step);
        return new Vector3(Sample(p - dx).Ground - Sample(p + dx).Ground, 2f * step,
            Sample(p - dz).Ground - Sample(p + dz).Ground).Normalized();
    }

    private float Accumulate(ref TerrainBlend blend, TileId id, float weight, Vector2 p)
    {
        if (!source.Cells.TryGetValue(id, out var cell)) return 0f;
        // 河道的宽度按到各条已连通边的距离计算，穿越共边时两侧使用同一中点。
        if (cell.Water.IsCenterline)
            blend.Channel = Mathf.Max(blend.Channel, RiverAt(id, cell.Water, p));
        blend.Total += weight;
        if (cell.StillWater)
        {
            blend.Wet += weight;
            blend.Level += cell.WaterHeight * weight;
            blend.Bed += cell.Height * weight;
            return 0f;
        }
        blend.Dry += weight;
        blend.Land += cell.Height * weight;
        blend.Color += cell.Color * weight;
        blend.Mountain += cell.Mountain * weight;
        blend.Snow += cell.Snow * weight;
        blend.Trees += cell.Trees * weight;
        if (cell.Mountain >= 1f) blend.Peak = Mathf.Max(blend.Peak, PeakAt(id, cell.Elevation, p));
        return cell.Mountain;
    }

    private float PeakAt(TileId id, float elevation, Vector2 p)
    {
        var center = Center(id);
        var offset = new Vector2(Noise(center + new Vector2(29f, 8f), .93f),
            Noise(center + new Vector2(7f, 40f), 1.31f)) * .26f;
        var delta = p - center - offset;
        float distance = delta.Length();
        if (distance >= 1.15f) return 0f;
        float phase = offset.X * 15f + offset.Y * 9f;
        float angle = delta.Angle();
        float ridgedRadius = .84f + .12f * Mathf.Sin(3 * angle + phase) + .13f * Mathf.Sin(5 * angle - phase * .65f);
        float crown = Mathf.Max(1f - distance / ridgedRadius, 0f);
        return Mathf.Pow(crown, .76f + Mathf.Abs(offset.Y) * 1.15f) * (.61f + elevation * 1.03f + .51f * offset.Y);
    }

    private float TrailAt(Vector2 point, float detail)
    {
        if (source.Trail is not { } trail) return 0f;
        float distance = float.PositiveInfinity;
        if (point.DistanceSquaredTo(trail[0]) < 40f)
        {
            for (int i = 1; i < trail.Count; i++)
            {
                Vector2 a = trail[i - 1], delta = trail[i] - a;
                float t = Mathf.Clamp((point - a).Dot(delta) / delta.LengthSquared(), 0f, 1f);
                distance = Mathf.Min(distance, point.DistanceTo(a + delta * t));
            }
        }
        if (source.Gate is TileId gate)
        {
            var local = point - Center(gate);
            if (local.Y > 1.32f && local.Y < 2.95f)
            {
                float bend = .28f * Mathf.Sin((local.Y - 1.32f) * 2.3f);
                float fade = Mathf.SmoothStep(2.67f, 2.95f, local.Y) * .2f;
                distance = Mathf.Min(distance, Mathf.Abs(local.X - bend) * .63f + fade);
            }
        }
        return 1f - Mathf.SmoothStep(.057f, .16f, distance + .024f * detail);
    }

    private static float RiverAt(TileId id, MapWater water, Vector2 p)
    {
        if (!water.Active) return 0f;
        var center = Center(id);
        if (p.DistanceSquaredTo(center) > 1.62f) return 0f;
        float distance = float.PositiveInfinity;
        var downstream = water.OutDirection >= 0 ? Center(id.Neighbor(water.OutDirection)) : center;
        var outlet = center.Lerp(downstream, .62f);
        for (int d = 0; d < 6; d++)
        {
            if ((water.IncomingMask & (1 << d)) == 0) continue;
            // 共边中点固定。转弯发生在格内，折线细分后向出口收束。
            var inlet = center.Lerp(Center(id.Neighbor(d)), .5f);
            var controlIn = inlet.Lerp(center, .72f);
            var controlOut = outlet.Lerp(center, .67f);
            var previous = inlet;
            for (int step = 1; step <= 7; step++)
            {
                float t = step / 7f, u = 1f - t;
                var next = u * u * u * inlet + 3f * t * u * u * controlIn +
                    3f * t * t * u * controlOut + t * t * t * outlet;
                distance = Mathf.Min(distance, SegmentDistance(p, previous, next));
                previous = next;
            }
        }
        if (water.IncomingMask == 0) distance = SegmentDistance(p, center, outlet);
        float width = .175f + .04f * Mathf.Clamp(water.Width, 1, 3);
        return 1f - Mathf.SmoothStep(width * .3f, width, distance);
    }

    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        var delta = b - a;
        float t = Mathf.Clamp((p - a).Dot(delta) / Mathf.Max(delta.LengthSquared(), .0001f), 0f, 1f);
        return p.DistanceTo(a + delta * t);
    }

    private static float PairRidge(float a, float b, float ma, float mb) => .49f * Mathf.Pow(4f * a * b, .87f) * ma * mb;
    private float Noise(Vector2 p, float scale) => _noise.GetNoise2D(p.X * scale, p.Y * scale);

    private struct TerrainBlend
    {
        internal float Total, Dry, Land, Wet, Level, Bed, Mountain, Snow, Channel, Trees, Peak;
        internal Color Color;
    }
}
