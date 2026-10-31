using Godot;
using System;

namespace Xianxia.Client.UI;

/// <summary>统一设计画布上的基础控件，不访问战斗数据。</summary>
internal static class ObserverWidgets
{
    public static readonly Color Ink = new("e8e3ce");
    public static readonly Color Muted = new("afc7c5");
    public static readonly Color PaperInk = new(0.2f, 0.22f, 0.17f);
    public static readonly Color PaperMuted = new(0.32f, 0.35f, 0.27f);

    public static Label Text(Control parent, string text, Rect2 rect, int size, Color color, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label { Text = text, Position = rect.Position, Size = rect.Size, HorizontalAlignment = align,
            VerticalAlignment = VerticalAlignment.Center, ClipText = true, MouseFilter = Control.MouseFilterEnum.Pass };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        parent.AddChild(label);
        return label;
    }

    public static Button Button(Control parent, ObserverPreviewSkin skin, string text, Rect2 rect, Action action)
    {
        var button = new Button { Text = text, Position = rect.Position, Size = rect.Size };
        button.AddThemeFontSizeOverride("font_size", 17);
        button.AddThemeStyleboxOverride("normal", skin.Dark);
        button.AddThemeColorOverride("font_color", Ink);
        button.AddThemeStyleboxOverride("hover", Style(new Color("3c6265")));
        button.AddThemeStyleboxOverride("pressed", Style(new Color("527879")));
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }

    public static Panel Panel(Control parent, Rect2 rect, StyleBox style)
    {
        var panel = new Panel { Position = rect.Position, Size = rect.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", style);
        parent.AddChild(panel);
        return panel;
    }

    public static TextureRect Image(Control parent, Texture2D? texture, Rect2 rect, bool keepAspect = false)
    {
        var image = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Texture = texture,
            Position = rect.Position, Size = rect.Size,
            StretchMode = keepAspect ? TextureRect.StretchModeEnum.KeepAspectCentered : TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        parent.AddChild(image);
        return image;
    }

    public static StyleBoxFlat Style(Color color) => new()
    {
        BgColor = color, BorderColor = new Color("9d9267", 0.8f), BorderWidthLeft = 1, BorderWidthTop = 1,
        BorderWidthRight = 1, BorderWidthBottom = 1, CornerRadiusTopLeft = 9, CornerRadiusTopRight = 9,
        CornerRadiusBottomLeft = 9, CornerRadiusBottomRight = 9
    };
}
