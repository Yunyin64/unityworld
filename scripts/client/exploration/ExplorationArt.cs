using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Xianxia.Client.Exploration;

/// <summary>Client-only art mappings shared by the map, selected-place thumbnail, and HUD.</summary>
public sealed class ExplorationArt
{
    internal const string Manifest = "res://assets/presentation/exploration_visuals.json";
    private readonly Dictionary<string, Texture2D?> _textures = new(StringComparer.Ordinal);
    private readonly HashSet<string> _warnings = new(StringComparer.Ordinal);
    private static readonly ConditionalWeakTable<Control, LandmarkShaderLayer> landmarkShaders = new();

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

    internal static Rect2 ReadRegion(JsonElement source) =>
        new(source[0].GetSingle(), source[1].GetSingle(), source[2].GetSingle(), source[3].GetSingle());

    private static bool Within(Texture2D texture, Rect2 region) =>
        region.Position.X >= 0 && region.Position.Y >= 0 && region.Size.X > 0 && region.Size.Y > 0 &&
        region.End.X <= texture.GetWidth() && region.End.Y <= texture.GetHeight();

    private static Texture2D? Crop(Texture2D? texture, Rect2 region) =>
        texture != null && Within(texture, region) ? new AtlasTexture { Atlas = texture, Region = region, FilterClip = true } : null;

    private Texture2D? LandMarkTexture(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        var image = GameBootstrap.Assets.Texture(address);
        if (image == null) Warn(address, "无法加载地标图片：" + address);
        return image;
    }

    private static void DrawLandmarkImage(Control canvas, Rect2 area, Texture2D image)
    {
        var original = image.GetSize();
        var fitted = original * Math.Min(area.Size.X / original.X, area.Size.Y / original.Y);
        canvas.DrawTextureRect(image, new Rect2(area.GetCenter() - fitted * 0.5f, fitted), false);
    }

    internal void DrawMapLandmarks(Control canvas, Vector2 center, float edge, ExplorationTile tile)
    {
        var marks = new (string Type, Texture2D? Image)[tile.Tile.LandMarkIds.Count];
        if (marks.Length == 0) return;
        marks[0] = (tile.LandmarkDrawType, LandmarkTexture(tile.LandmarkDrawType, tile.LandmarkImage));
        for (int index = 1; index < marks.Length; index++)
        {
            string type = index <= tile.AdditionalLandmarkDrawTypes.Count ? tile.AdditionalLandmarkDrawTypes[index - 1] : "Icon";
            string image = index <= tile.AdditionalLandmarkImages.Count ? tile.AdditionalLandmarkImages[index - 1] : "";
            marks[index] = (type, LandmarkTexture(type, image));
        }
        DrawMapLandmarks(canvas, center, edge, marks);
    }

    private Texture2D? LandmarkTexture(string type, string address) =>
        type is "Icon" or "TerrainMask" ? LandMarkTexture(address) : null;

    internal static void ClearLandmarkShaders(Control canvas)
    {
        if (landmarkShaders.TryGetValue(canvas, out var layer)) RenderingServer.CanvasItemClear(layer.Item);
    }

    internal static void DrawMapLandmarks(Control canvas, Vector2 center, float edge, IReadOnlyList<(string Type, Texture2D? Image)> marks)
    {
        foreach (var (type, image) in marks)
            DrawLandmarkEffect(canvas, center, edge, type, image);
        DrawIconLandmarks(canvas, center, edge, marks.Where(mark => mark.Type == "Icon").Select(mark => mark.Image).ToArray());
    }

    private static void DrawLandmarkEffect(Control canvas, Vector2 center, float edge, string type, Texture2D? image)
    {
        if (type == "TerrainMask")
        {
            var polygon = ExplorationHexProjection.Outline(center, edge * .94f);
            var tint = new Color(.63f, .77f, .66f, .48f);
            if (image == null) canvas.DrawColoredPolygon(polygon, tint);
            else canvas.DrawColoredPolygon(polygon, tint, LandmarkUvs(polygon, center, edge), image);
        }
        else if (type == "Atmosphere")
        {
            for (int radius = 9; radius >= 1; radius--)
                canvas.DrawCircle(center - Vector2.Up * edge * .1f, edge * radius * .091f, new Color(.7f, .84f, .92f, .045f));
        }
        else if (type == "Shader")
        {
            var polygon = ExplorationHexProjection.Outline(center, edge * .89f);
            var layer = landmarkShaders.GetValue(canvas, owner => new LandmarkShaderLayer(owner));
            RenderingServer.CanvasItemAddPolygon(layer.Item, polygon, [Colors.White], LandmarkUvs(polygon, center, edge));
        }
    }

    private static Vector2[] LandmarkUvs(Vector2[] polygon, Vector2 center, float edge) =>
        polygon.Select(point => (point - center) / (edge * 2f) + Vector2.One * .5f).ToArray();

    // 独立画布项给地标提供材质，地图自身的地形和标记沿用原有着色。
    private sealed class LandmarkShaderLayer
    {
        private readonly ShaderMaterial material = new()
        {
            Shader = new Shader
            {
                Code = """
                    shader_type canvas_item;
                    render_mode unshaded;
                    void fragment() {
                        float r = length(UV - vec2(0.5));
                        float pulse = 0.55 + 0.45 * sin(TIME * 2.0 - 25.0 * r);
                        float fade = 1.0 - smoothstep(0.05, 0.44, r);
                        COLOR *= vec4(0.48, 0.86, 0.76, fade * pulse * 0.58);
                    }
                    """
            }
        };

        internal Rid Item { get; } = RenderingServer.CanvasItemCreate();

        internal LandmarkShaderLayer(Control owner)
        {
            RenderingServer.CanvasItemSetParent(Item, owner.GetCanvasItem());
            RenderingServer.CanvasItemSetMaterial(Item, material.GetRid());
            owner.TreeExiting += () =>
            {
                RenderingServer.FreeRid(Item);
                landmarkShaders.Remove(owner);
            };
        }
    }

    // 两个地图视图都以实际六边形中心和边长为输入，不在图上叠编辑器底座。
    private static void DrawIconLandmarks(Control canvas, Vector2 center, float edge, IReadOnlyList<Texture2D?> images)
    {
        if (images.Count == 0) return;
        int shown = 1;
        if (images.Any(image => image != null))
        {
            int limit = edge >= 38 ? 3 : edge >= 24 ? 2 : 1;
            var visible = images.Count <= limit ? images : images.OrderByDescending(image => image != null).Take(limit).ToArray();
            shown = visible.Count;
            // 单地标沿用探索地图原有矩形锚点；多图时共用同一个排布基准。
            float width = edge * (shown == 1 ? 1.28f : shown == 2 ? 0.82f : 0.57f);
            float height = edge * (shown == 1 ? 1.05f : shown == 2 ? 0.87f : 0.73f);
            var size = new Vector2(width, height);
            for (int index = 0; index < shown; index++)
            {
                var at = center + new Vector2((index - (shown - 1) / 2f) * width, -edge * 0.075f);
                if (visible[index] is { } image) DrawLandmarkImage(canvas, new Rect2(at - size / 2, size), image);
                else DrawLandmarkLabel(canvas, at, "地", edge);
            }
        }
        else DrawLandmarkLabel(canvas, center + new Vector2(edge * 0.27f, -edge * 0.16f), "地", edge);

        if (images.Count > shown)
            DrawLandmarkLabel(canvas, center + new Vector2(edge * 0.48f, edge * 0.43f), $"+{images.Count - shown}", edge);
    }

    private static void DrawLandmarkLabel(Control canvas, Vector2 center, string label, float edge)
    {
        int size = (int)Mathf.Clamp(edge * 0.30f, 9, 17);
        var font = canvas.GetThemeDefaultFont();
        var position = center + new Vector2(-font.GetStringSize(label, fontSize: size).X / 2, size * 0.33f);
        canvas.DrawStringOutline(font, position, label, fontSize: size, size: 4, modulate: new Color("17241d"));
        canvas.DrawString(font, position, label, fontSize: size, modulate: new Color("f3df9a"));
    }

    internal void DrawGround(Control canvas, Vector2[] points, ExplorationTile tile, int seed, int plane)
    {
        if (GameBootstrap.Assets.Terrain.Resolve(tile.Tile.TypeId, tile.TerrainAssetRef, tile.Tile.Id, seed, plane) is { } material)
        {
            material.DrawHex(canvas, points);
            return;
        }
        canvas.DrawColoredPolygon(points, Color.FromString(tile.Color, Colors.Gray));
        var center = (points[0] + points[3]) * 0.5f;
        DrawFallbackName(canvas, new Rect2(points[3].X, points[5].Y, points[0].X - points[3].X,
            (center.Y - points[5].Y) * 2), tile);
    }

    /// <summary>按探索中的位置裁切地形缩略图，并叠加该格的地标。</summary>
    public void DrawThumbnail(Control canvas, Rect2 area, ExplorationTile tile, int seed, int plane)
    {
        if (!area.HasArea()) return;
        if (GameBootstrap.Assets.Terrain.Resolve(tile.Tile.TypeId, tile.TerrainAssetRef, tile.Tile.Id, seed, plane) is { } material)
            material.DrawThumbnail(canvas, area);
        else
        {
            canvas.DrawRect(area, Color.FromString(tile.Color, Colors.Gray));
            DrawFallbackName(canvas, area, tile);
        }
        if (tile.LandmarkDrawType == "Icon" && LandMarkTexture(tile.LandmarkImage) is { } image)
            DrawLandmarkImage(canvas, area.Grow(-4f), image);
        else if (tile.Tile.LandMarkId.HasValue)
            DrawLandmarkEffect(canvas, area.GetCenter(), Math.Min(area.Size.X, area.Size.Y) * .46f, tile.LandmarkDrawType,
                LandmarkTexture(tile.LandmarkDrawType, tile.LandmarkImage));
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
