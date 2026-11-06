using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Xianxia.Client.UI.Templates;

public sealed record TemplateVisualMapping(string ActorVisualKey, string PortraitKey);

/// <summary>Private editor theme and presentation-only resource lookup.</summary>
public sealed class TemplateEditorSkin
{
    public Theme Theme { get; }
    public StyleBox PanelStyle { get; }
    public StyleBoxFlat Button { get; }
    public StyleBoxFlat SelectedButton { get; }
    public StyleBoxFlat Keyword { get; }
    public StyleBoxFlat SelectedKeyword { get; }
    public StyleBoxFlat Input { get; }
    public StyleBoxFlat Row { get; }
    public StyleBoxFlat SelectedRow { get; }
    public Texture2D? Background { get; }
    public Color PaperText { get; }
    public Color PaperMuted { get; }
    public Color DarkText { get; }
    public IReadOnlyList<string> ActorKeys => _actors.Keys.ToArray();
    public IReadOnlyList<string> PortraitKeys => _portraits.Keys.ToArray();
    private readonly Dictionary<string, Texture2D> _actors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Texture2D> _portraits = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TemplateVisualMapping> _templates = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StyleBoxFlat> _states = new(StringComparer.Ordinal);

    public TemplateEditorSkin()
    {
        using var document = JsonDocument.Parse(Godot.FileAccess.GetFileAsString("res://assets/presentation/template_editor_visuals.json"));
        var root = document.RootElement;
        var typography = root.GetProperty("typography");
        PaperText = ReadColor(typography, "paperText", "33382B");
        PaperMuted = ReadColor(typography, "paperMuted", "525945");
        DarkText = ReadColor(typography, "darkText", "E8E3CE");
        Theme = new Theme
        {
            DefaultFont = new SystemFont { FontNames = typography.GetProperty("fontNames").EnumerateArray().Select(x => x.GetString()!).ToArray() },
            DefaultFontSize = typography.GetProperty("bodySize").GetInt32()
        };
        Background = LoadTexture(root.GetProperty("background"));
        var panel = root.GetProperty("panels").GetProperty("editor");
        var texture = LoadTexture(panel);
        if (texture != null)
        {
            using var image = texture.GetImage();
            if (image.IsCompressed()) image.Decompress();
            var size = panel.GetProperty("displayTextureSize").EnumerateArray().Select(v => v.GetInt32()).ToArray();
            image.Resize(size[0], size[1], Image.Interpolation.Lanczos);
            var box = new StyleBoxTexture { Texture = ImageTexture.CreateFromImage(image) };
            foreach (var (side, name) in Sides)
            {
                box.SetTextureMargin(side, panel.GetProperty("displayNineSliceMargins").GetProperty(name).GetSingle());
                box.SetContentMargin(side, panel.GetProperty("contentMargins").GetProperty(name).GetSingle());
            }
            PanelStyle = box;
        }
        else PanelStyle = Flat(new Color("DCCC9D"), new Color("8B774A"), 2, 5);
        var styles = root.GetProperty("styles");
        foreach (string group in new[] { "button", "keyword", "templateRow" })
            foreach (var state in styles.GetProperty(group).EnumerateObject())
                if (state.Value.ValueKind == JsonValueKind.Object)
                    _states[group + ":" + state.Name] = State(state.Value);
        Button = _states["button:normal"];
        SelectedButton = _states["button:selected"];
        Keyword = _states["keyword:normal"];
        SelectedKeyword = _states["keyword:selected"];
        Input = Flat(new Color("293633EB"), new Color("9D9267"), 1, 5);
        Row = _states["templateRow:normal"];
        SelectedRow = _states["templateRow:selected"];
        foreach (string control in new[] { "Button", "OptionButton" })
        {
            Theme.SetStylebox("normal", control, Button);
            Theme.SetStylebox("hover", control, _states["button:hover"]);
            Theme.SetStylebox("pressed", control, _states["button:pressed"]);
            Theme.SetStylebox("disabled", control, _states["button:disabled"]);
            Theme.SetStylebox("focus", control, _states["button:focus"]);
            Theme.SetColor("font_color", control, DarkText);
            Theme.SetColor("font_hover_color", control, DarkText);
            Theme.SetColor("font_pressed_color", control, DarkText);
            Theme.SetColor("font_disabled_color", control, new Color("A1A497"));
        }
        Theme.SetColor("font_color", "Label", PaperText);
        foreach (string control in new[] { "LineEdit", "TextEdit" })
        {
            Theme.SetStylebox("normal", control, Input);
            Theme.SetStylebox("read_only", control, _states["button:disabled"]);
            Theme.SetStylebox("focus", control, _states["button:focus"]);
            Theme.SetColor("font_color", control, DarkText);
            Theme.SetColor("font_placeholder_color", control, new Color("A9AC98"));
            Theme.SetColor("caret_color", control, DarkText);
        }
        Theme.SetStylebox("panel", "PopupMenu", Flat(new Color("293633"), new Color("B0A373"), 1, 5));
        Theme.SetColor("font_color", "PopupMenu", DarkText);
        Theme.SetColor("font_hover_color", "PopupMenu", new Color("FFF6D9"));
        LoadMap(root.GetProperty("actors"), _actors);
        LoadMap(root.GetProperty("portraits"), _portraits);
        foreach (var entry in root.GetProperty("templates").EnumerateObject())
            _templates[entry.Name] = new(entry.Value.GetProperty("actorVisualKey").GetString()!, entry.Value.GetProperty("portraitKey").GetString()!);
    }

    public Texture2D? Actor(string? key) => key != null && _actors.TryGetValue(key, out var value) ? value : null;
    public Texture2D? Portrait(string? key) => key != null && _portraits.TryGetValue(key, out var value) ? value : null;
    public TemplateVisualMapping? LookupTemplate(string? id) => id != null && _templates.TryGetValue(id, out var value) ? value : null;

    public void StyleButton(Godot.Button control, bool selected = false, bool keyword = false)
    {
        string group = keyword ? "keyword:" : "button:";
        control.AddThemeStyleboxOverride("normal", _states[group + (selected ? "selected" : "normal")]);
        control.AddThemeStyleboxOverride("hover", _states[group + (selected ? "selected" : "hover")]);
        control.AddThemeStyleboxOverride("pressed", _states[group + "pressed"]);
        control.AddThemeStyleboxOverride("disabled", _states[group + "disabled"]);
        control.AddThemeStyleboxOverride("focus", _states[group + "focus"]);
        var color = keyword && !selected ? PaperText : DarkText;
        control.AddThemeColorOverride("font_color", color);
        control.AddThemeColorOverride("font_hover_color", color);
        control.AddThemeColorOverride("font_pressed_color", DarkText);
    }

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

    private static readonly (Side Side, string Name)[] Sides = { (Side.Left, "left"), (Side.Top, "top"), (Side.Right, "right"), (Side.Bottom, "bottom") };
    private static Color ReadColor(JsonElement value, string property, string fallback) =>
        new(value.TryGetProperty(property, out var color) ? color.GetString()!.TrimStart('#') : fallback);
    private static StyleBoxFlat State(JsonElement value)
    {
        var box = Flat(ReadColor(value, "fill", "00000000"), ReadColor(value, "border", "B0A373"),
            value.TryGetProperty("borderWidth", out var width) ? width.GetInt32() : 1);
        return box;
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
