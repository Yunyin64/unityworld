using Godot;
using System.Collections.Generic;
using System.Linq;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>战报折叠、滚动历史与最近摘要；不拥有文件和战斗生命周期。</summary>
internal sealed class ObserverLogPanel
{
    private readonly RichTextLabel _compact, _expanded;
    private readonly Control _overlay;
    private IReadOnlyList<BattleRecord> _records = System.Array.Empty<BattleRecord>();
    private int _cursor;
    private readonly Button _expand;
    public Button CompactShortcut { get; }
    public Button ExpandButton => CompactShortcut.Visible ? CompactShortcut : _expand;
    public Button CloseButton { get; }
    public bool IsExpanded => _overlay.Visible;

    public ObserverLogPanel(Control canvas, Control log, Control history)
    {
        _compact = log.GetNode<RichTextLabel>("Recent");
        _overlay = history;
        _expanded = _overlay.GetNode<RichTextLabel>("Paper/Entries");
        _expand = log.GetNode<Button>("Expand");
        CompactShortcut = canvas.GetNode<Button>("CompactLog");
        CompactShortcut.Pressed += () => Toggle(true);
        CloseButton = _overlay.GetNode<Button>("Paper/Close");
        _expand.Pressed += () => Toggle(true);
        CloseButton.Pressed += () => Toggle(false);
        Reset();
    }

    /// <summary>同窗重新开场时，旧战报的显示游标不能跨入新会话。</summary>
    public void Reset()
    {
        _records = System.Array.Empty<BattleRecord>();
        _cursor = 0;
        _compact.Text = _compact.TooltipText = _expanded.Text = "";
        _overlay.Visible = false;
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
