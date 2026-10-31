using Godot;
using System.Collections.Generic;
using System.Linq;
using Xianxia.Client.Battle;
using static Xianxia.Client.UI.ObserverWidgets;

namespace Xianxia.Client.UI;

/// <summary>战报折叠、滚动历史与最近摘要；不拥有文件和战斗生命周期。</summary>
internal sealed class ObserverLogPanel
{
    private readonly RichTextLabel _compact, _expanded;
    private readonly Control _overlay;
    private IReadOnlyList<BattleRecord> _records = System.Array.Empty<BattleRecord>();
    private int _cursor;
    public Button ExpandButton { get; }
    public Button CloseButton { get; }
    public bool IsExpanded => _overlay.Visible;

    public ObserverLogPanel(Control canvas, ObserverPreviewSkin skin)
    {
        Image(canvas, skin.Log, new Rect2(486, 750, 628, 133));
        Text(canvas, "战斗记录", new Rect2(536, 768, 425, 28), 21, PaperInk);
        ExpandButton = Button(canvas, skin, "展开", new Rect2(1000, 771, 82, 26), () => Toggle(true));
        ExpandButton.AddThemeFontSizeOverride("font_size", 14);
        _compact = new RichTextLabel { Position = new Vector2(536, 803), Size = new Vector2(540, 54),
            BbcodeEnabled = false, ScrollActive = false, AutowrapMode = TextServer.AutowrapMode.Off, SelectionEnabled = true };
        _compact.AddThemeFontSizeOverride("normal_font_size", 15);
        _compact.AddThemeColorOverride("default_color", PaperInk);
        canvas.AddChild(_compact);
        _overlay = new Control { Size = new Vector2(1600, 900), Visible = false, ZIndex = 20 };
        canvas.AddChild(_overlay);
        _overlay.AddChild(new ColorRect { Size = new Vector2(1600, 900), Color = new Color(0.03f, 0.07f, 0.08f, 0.66f) });
        Panel(_overlay, new Rect2(240, 130, 1120, 630), skin.Header);
        Text(_overlay, "战斗记录 · 完整历史可滚动查看", new Rect2(275, 151, 920, 42), 25, PaperInk);
        CloseButton = Button(_overlay, skin, "收起", new Rect2(1235, 153, 90, 38), () => Toggle(false));
        _expanded = new RichTextLabel { Position = new Vector2(275, 214), Size = new Vector2(1050, 510),
            BbcodeEnabled = false, ScrollFollowing = true, SelectionEnabled = true };
        _expanded.AddThemeFontSizeOverride("normal_font_size", 18);
        _expanded.AddThemeColorOverride("default_color", PaperInk);
        _overlay.AddChild(_expanded);
    }

    public void Submit(IReadOnlyList<BattleRecord> records)
    {
        _records = records;
        if (_cursor == records.Count) return;
        if (_overlay.Visible)
        {
            while (_cursor < records.Count) _expanded.AppendText(Format(records[_cursor++]) + "\n");
        }
        _cursor = records.Count;
        _compact.Text = string.Join("\n", records.TakeLast(2).Select(Compact));
        _compact.TooltipText = string.Join("\n", records.TakeLast(2).Select(Format));
    }

    private void Toggle(bool expanded)
    {
        _overlay.Visible = expanded;
        if (expanded) _expanded.Text = string.Join("\n", _records.Select(Format)) + "\n";
    }

    private static string Format(BattleRecord record) => $"[T{record.Tick} #{record.Sequence} {record.Source}] {record.Text}";
    private string Compact(BattleRecord record)
    {
        string text = $"T{record.Tick} · {record.Text.Replace('\n', ' ').Replace('\r', ' ')}";
        var font = _compact.GetThemeDefaultFont();
        if (font.GetStringSize(text, fontSize: 15).X <= 512) return text;
        while (text.Length > 0 && font.GetStringSize(text + "…", fontSize: 15).X > 512) text = text[..^1];
        return text + "…";
    }
}
