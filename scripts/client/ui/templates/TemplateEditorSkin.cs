using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Xianxia.Client.UI.Templates;

public sealed record TemplateVisualMapping(string ActorVisualKey, string PortraitKey);

/// <summary>展示用角色素材映射；固定布局和主题保存在模板场景资源中。</summary>
public sealed class TemplateEditorSkin
{
    private readonly Dictionary<string, Texture2D> _actors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Texture2D> _portraits = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TemplateVisualMapping> _templates = new(StringComparer.Ordinal);

    public TemplateEditorSkin()
    {
        using var document = JsonDocument.Parse(Godot.FileAccess.GetFileAsString("res://assets/presentation/template_editor_visuals.json"));
        var root = document.RootElement;
        LoadMap(root.GetProperty("actors"), _actors);
        LoadMap(root.GetProperty("portraits"), _portraits);
        foreach (var entry in root.GetProperty("templates").EnumerateObject())
            _templates[entry.Name] = new(entry.Value.GetProperty("actorVisualKey").GetString()!, entry.Value.GetProperty("portraitKey").GetString()!);
    }

    public Texture2D? Actor(string? key) => key != null && _actors.TryGetValue(key, out var value) ? value : null;
    public Texture2D? Portrait(string? key) => key != null && _portraits.TryGetValue(key, out var value) ? value : null;
    public TemplateVisualMapping? LookupTemplate(string? id) => id != null && _templates.TryGetValue(id, out var value) ? value : null;

    public static StyleBoxFlat Flat(Color fill, Color border, int width = 1, int radius = 5) => new()
    {
        BgColor = fill, BorderColor = border,
        BorderWidthLeft = width, BorderWidthRight = width, BorderWidthTop = width, BorderWidthBottom = width,
        CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius, CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
        ContentMarginLeft = 9, ContentMarginRight = 9, ContentMarginTop = 5, ContentMarginBottom = 5
    };

    internal static void DrawFit(Control view, Texture2D texture, Rect2 area, bool transpose = false, float alpha = 1)
    {
        var original = texture.GetSize();
        var source = transpose ? new Vector2(original.Y, original.X) : original;
        float scale = Math.Min(area.Size.X / Math.Max(1, source.X), area.Size.Y / Math.Max(1, source.Y));
        if (scale <= 0) return;
        if (transpose)
        {
            view.DrawSetTransform(area.GetCenter(), -Mathf.Pi / 2);
            view.DrawTextureRect(texture, new Rect2(-original * scale / 2, original * scale), false, new Color(1, 1, 1, alpha));
            view.DrawSetTransform(Vector2.Zero);
        }
        else view.DrawTextureRect(texture, new Rect2(area.GetCenter() - source * scale / 2, source * scale), false, new Color(1, 1, 1, alpha));
    }

    private static void LoadMap(JsonElement values, Dictionary<string, Texture2D> target)
    {
        foreach (var entry in values.EnumerateObject())
            if (LoadTexture(entry.Value) is { } texture) target[entry.Name] = texture;
    }
    private static Texture2D? LoadTexture(JsonElement entry)
    {
        string? path = entry.GetProperty("texture").GetString();
        if (path == null || !ResourceLoader.Exists(path)) return null;
        var texture = GD.Load<Texture2D>(path);
        if (texture == null) return null;
        if (entry.TryGetProperty("region", out var region) && region.ValueKind == JsonValueKind.Array)
        {
            var values = region.EnumerateArray().Select(v => v.GetSingle()).ToArray();
            return new AtlasTexture { Atlas = texture, Region = new Rect2(values[0], values[1], values[2], values[3]) };
        }
        return texture;
    }
}
