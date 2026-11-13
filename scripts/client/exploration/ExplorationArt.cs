using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.Exploration;

/// <summary>Client-only art mappings shared by the map, selected-place thumbnail, and HUD.</summary>
public sealed class ExplorationArt
{
    private const string Manifest = "res://assets/presentation/exploration_visuals.json";
    private readonly Dictionary<string, Texture2D?> _textures = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (Texture2D Texture, Rect2[] Regions)> _terrain = new(StringComparer.Ordinal);
    private readonly HashSet<string> _warnings = new(StringComparer.Ordinal);
    private static readonly Vector2[] HexUv =
    {
        new(1, 0.5f), new(0.75f, 1), new(0.25f, 1), new(0, 0.5f), new(0.25f, 0), new(0.75f, 0)
    };

    public Texture2D? Actor { get; }
    public Texture2D? Portrait { get; }

    public ExplorationArt()
    {
        using var document = JsonDocument.Parse(FileAccess.GetFileAsString(Manifest));
        var config = document.RootElement;
        var actor = config.GetProperty("actor");
        var actorTexture = Texture(actor);
        Actor = Crop(actorTexture, ReadRegion(actor.GetProperty("region")));
        Portrait = Crop(actorTexture, ReadRegion(actor.GetProperty("portraitRegion")));
        foreach (var item in config.GetProperty("terrain").EnumerateObject())
        {
            var entry = item.Value;
            var texture = Texture(entry);
            if (texture == null) continue;
            var regions = new List<Rect2> { ReadRegion(entry.GetProperty("region")) };
            regions.AddRange(entry.GetProperty("variants").EnumerateArray().Select(ReadRegion));
            float scale = entry.GetProperty("displayScale").GetSingle();
            if (!float.IsFinite(scale) || scale < 1 || regions.Any(region => !Within(texture, region)))
            {
                Warn(item.Name, "地形图裁切或缩放无效：" + item.Name);
                continue;
            }
            _terrain[item.Name] = (texture, regions.Select(region =>
                new Rect2(region.GetCenter() - region.Size / (2f * scale), region.Size / scale)).ToArray());
        }
    }

    private Texture2D? Texture(JsonElement entry)
    {
        string path = entry.GetProperty("texture").GetString() ?? "";
        if (_textures.TryGetValue(path, out var value)) return value;
        value = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
        if (value == null) Warn(path, "无法加载探索素材：" + path);
        _textures[path] = value;
        return value;
    }

    private void Warn(string key, string message)
    {
        if (_warnings.Add(key)) GD.PushWarning(message);
    }

    private static Rect2 ReadRegion(JsonElement source) =>
        new(source[0].GetSingle(), source[1].GetSingle(), source[2].GetSingle(), source[3].GetSingle());

    private static bool Within(Texture2D texture, Rect2 region) =>
        region.Position.X >= 0 && region.Position.Y >= 0 && region.Size.X > 0 && region.Size.Y > 0 &&
        region.End.X <= texture.GetWidth() && region.End.Y <= texture.GetHeight();

    private static Texture2D? Crop(Texture2D? texture, Rect2 region) =>
        texture != null && Within(texture, region) ? new AtlasTexture { Atlas = texture, Region = region, FilterClip = true } : null;

    private bool TryMaterial(ExplorationTile tile, int seed, int plane, out Texture2D? texture, out Rect2 region)
    {
        if (_terrain.TryGetValue(tile.Tile.TypeId, out var entry))
        {
            // Coordinate-derived variants must survive pan, re-centering and a fresh process.
            uint hash = 2166136261;
            unchecked
            {
                hash = (hash ^ (uint)seed) * 16777619;
                hash = (hash ^ (uint)plane) * 16777619;
                hash = (hash ^ (uint)tile.Tile.Id.Q) * 16777619;
                hash = (hash ^ (uint)tile.Tile.Id.R) * 16777619;
            }
            texture = entry.Texture;
            region = entry.Regions[hash % (uint)entry.Regions.Length];
            return true;
        }
        Warn("missing/" + tile.Tile.TypeId, "探索地形缺少有效素材，使用核心颜色和符号：" + tile.Tile.TypeId);
        texture = null;
        region = default;
        return false;
    }

    internal void DrawGround(Control canvas, Vector2[] points, ExplorationTile tile, int seed, int plane)
    {
        if (!TryMaterial(tile, seed, plane, out var texture, out var region))
        {
            canvas.DrawColoredPolygon(points, Color.FromString(tile.Color, Colors.Gray));
            var center = (points[0] + points[3]) * 0.5f;
            DrawFallbackName(canvas, new Rect2(points[3].X, points[5].Y, points[0].X - points[3].X,
                (center.Y - points[5].Y) * 2), tile);
            return;
        }
        var uv = new Vector2[6];
        var textureSize = texture!.GetSize();
        for (int i = 0; i < uv.Length; i++) uv[i] = (region.Position + region.Size * HexUv[i]) / textureSize;
        canvas.DrawColoredPolygon(points, Colors.White, uv, texture);
    }

    public void DrawThumbnail(Control canvas, Rect2 area, ExplorationTile tile, int seed, int plane)
    {
        if (!area.HasArea()) return;
        if (!TryMaterial(tile, seed, plane, out var texture, out var region))
        {
            canvas.DrawRect(area, Color.FromString(tile.Color, Colors.Gray));
            DrawFallbackName(canvas, area, tile);
            return;
        }
        float cover = Math.Max(area.Size.X / region.Size.X, area.Size.Y / region.Size.Y);
        var crop = new Rect2(region.GetCenter() - area.Size / cover * 0.5f, area.Size / cover);
        canvas.DrawTextureRectRegion(texture!, area, crop);
    }

    private static void DrawFallbackName(Control canvas, Rect2 area, ExplorationTile tile)
    {
        string label = string.IsNullOrWhiteSpace(tile.Symbol) ? tile.Name : tile.Symbol;
        var origin = new Vector2(area.Position.X, area.GetCenter().Y + 5);
        var font = canvas.GetThemeDefaultFont();
        canvas.DrawStringOutline(font, origin, label, HorizontalAlignment.Center, area.Size.X, 14, 3, Colors.Black);
        canvas.DrawString(font, origin, label, HorizontalAlignment.Center, area.Size.X, 14, Colors.White);
    }
}
