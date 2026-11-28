using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.Exploration;

/// <summary>探索地图、地点缩略图和区域编辑器共用的地形素材及绘制规则。</summary>
internal sealed class TerrainArt
{
    private const string AtlasPrefix = "atlas:";
    private static readonly Vector2[] HexUv =
    [
        new(1, 0.5f), new(0.75f, 1), new(0.25f, 1), new(0, 0.5f), new(0.25f, 0), new(0.75f, 0)
    ];

    internal readonly record struct Material(Texture2D Texture, Rect2 Region)
    {
        internal void DrawHex(Control canvas, Vector2[] points)
        {
            var uv = new Vector2[HexUv.Length];
            Vector2 textureSize = Texture.GetSize();
            for (int index = 0; index < uv.Length; index++)
                uv[index] = (Region.Position + Region.Size * HexUv[index]) / textureSize;
            canvas.DrawColoredPolygon(points, Colors.White, uv, Texture);
        }

        internal void DrawThumbnail(Control canvas, Rect2 area)
        {
            if (area.HasArea()) canvas.DrawTextureRectRegion(Texture, area, Cover(area.Size));
        }

        private Rect2 Cover(Vector2 size)
        {
            float scale = Mathf.Max(size.X / Region.Size.X, size.Y / Region.Size.Y);
            Vector2 cropSize = size / scale;
            return new Rect2(Region.GetCenter() - cropSize / 2, cropSize);
        }
    }

    private readonly ClientAssets assets;
    private readonly Dictionary<string, Material[]> atlases = new(StringComparer.Ordinal);
    private readonly HashSet<string> warnings = new(StringComparer.Ordinal);

    internal TerrainArt(ClientAssets assets)
    {
        this.assets = assets;
        using var manifest = JsonDocument.Parse(FileAccess.GetFileAsString(ExplorationArt.Manifest));
        var byImage = new Dictionary<string, List<(string Id, Rect2[] Regions)>>(StringComparer.Ordinal);
        foreach (var item in manifest.RootElement.GetProperty("terrain").EnumerateObject())
        {
            var entry = item.Value;
            string path = entry.GetProperty("texture").GetString() ?? "";
            var regions = new List<Rect2> { ExplorationArt.ReadRegion(entry.GetProperty("region")) };
            regions.AddRange(entry.GetProperty("variants").EnumerateArray().Select(ExplorationArt.ReadRegion));
            float scale = entry.GetProperty("displayScale").GetSingle();
            if (!float.IsFinite(scale) || scale < 1 || regions.Any(region =>
                region.Position.X < 0 || region.Position.Y < 0 || !region.HasArea()))
            {
                Warn("atlas/" + item.Name, $"地形图集条目 {item.Name} 的裁切或缩放无效：{path}");
                continue;
            }
            if (!byImage.TryGetValue(path, out var entries)) byImage.Add(path, entries = new());
            entries.Add((item.Name, regions.Select(region =>
                new Rect2(region.GetCenter() - region.Size / (2 * scale), region.Size / scale)).ToArray()));
        }
        foreach (var (path, entries) in byImage)
        {
            // 一张覆盖图集必须同时容纳使用它的所有条目，避免单张图混用两个来源。
            var regions = entries.SelectMany(entry => entry.Regions);
            var required = new Vector2(regions.Max(region => region.End.X), regions.Max(region => region.End.Y));
            if (assets.Texture(path, minimumSize: required) is not { } texture)
            {
                Warn("atlas-file/" + path, $"地形图集缺少可用图片：{path}");
                continue;
            }
            foreach (var (id, crops) in entries)
                atlases[AtlasPrefix + id] = crops.Select(region => new Material(texture, region)).ToArray();
        }
    }

    internal Material? Resolve(string id, string assetRef, TileId? cell = null, int seed = 0, int plane = 0)
    {
        string address = assetRef.Trim();
        if (atlases.TryGetValue(address, out var artwork))
            return Pick(artwork, cell, seed, plane);

        if (address.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && assets.Texture(address) is { } texture)
            return new Material(texture, new Rect2(Vector2.Zero, texture.GetSize()));

        if (address.Length > 0)
            Warn($"reference/{id}/{address}", $"地形 {id} 的 AssetRef 不可用：{address}。尝试同 ID 图集。");
        if (atlases.TryGetValue(AtlasPrefix + id, out var fallback)) return Pick(fallback, cell, seed, plane);
        Warn("missing/" + id, $"地形 {id} 缺少图片，使用地块颜色和字标。");
        return null;
    }

    private static Material Pick(Material[] variants, TileId? cell, int seed, int plane)
    {
        if (cell is not { } position) return variants[0];
        // 同一坐标在缩放、平移和重新打开时选到相同的裁切。
        uint hash = 2166136261;
        unchecked
        {
            hash = (hash ^ (uint)seed) * 16777619;
            hash = (hash ^ (uint)plane) * 16777619;
            hash = (hash ^ (uint)position.Q) * 16777619;
            hash = (hash ^ (uint)position.R) * 16777619;
        }
        return variants[hash % (uint)variants.Length];
    }

    private void Warn(string key, string message)
    {
        if (warnings.Add(key)) GD.PushWarning(message);
    }
}
