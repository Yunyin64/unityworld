using Godot;
using System;

namespace Xianxia.Client.UI.Templates;

/// <summary>模板页中按数据生成的列表、详情和诊断控件，共用同一套文字与选中样式。</summary>
internal static class TemplateEditorWidgets
{
    internal static Button FlowButton(Control parent, string title, Action action, bool selected = false, int height = 34)
    {
        var button = new Button
        {
            Text = title,
            CustomMinimumSize = new(0, height),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ClipText = true,
            ThemeTypeVariation = selected ? "SelectedKeyword" : "KeywordButton"
        };
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }

    internal static Label FlowText(Control parent, string text, int size = 18, bool wrap = true)
    {
        var label = new Label
        {
            Text = text,
            CustomMinimumSize = new(0, 28),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off
        };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color("33382b"));
        parent.AddChild(label);
        return label;
    }

    internal static void Clear(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }

    internal static string DefinitionName(DefineBase definition) =>
        string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.ID : definition.DisplayName;
}
