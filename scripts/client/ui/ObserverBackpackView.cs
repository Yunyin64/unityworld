using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>只消费值快照的背包视图。扫描光效不能触发核心行为。</summary>
public partial class ObserverBackpackView : Control
{
    private NpcSnapshot? _npc;
    private ObserverArtCatalog? _art;
    private StyleBoxFlat? _cellStyle;
    private readonly Dictionary<string, StyleBoxFlat> _cardStyles = new();
    private int _ticksPerRow = 10;
    private float _fromLine;
    private float _targetLine;
    private float _line;
    private float _elapsed;
    private bool _playing;
    private readonly Dictionary<int, int> _used = new();
    private readonly List<ManaSnapshot> _removed = new();

    /// <summary>仅供表现验收记录的当前显示位置，不是核心格间事件。</summary>
    public float ScanLineFraction => _line;

    /// <summary>给视图提供纯客户端资源映射。</summary>
    public void Configure(ObserverArtCatalog art, StyleBoxFlat? cellStyle = null)
    {
        _art = art;
        _cellStyle = cellStyle;
        foreach (string element in new[] { "Jin", "Mu", "Shui", "Huo", "Tu", "Wound" })
        {
            var style = cellStyle == null ? new StyleBoxFlat() : (StyleBoxFlat)cellStyle.Duplicate();
            style.BgColor = element switch
            {
                "Jin" => new Color("d6bc75"), "Mu" => new Color("91b9a0"), "Shui" => new Color("8db7c6"),
                "Huo" => new Color("d99b78"), "Tu" => new Color("c9b082"), _ => new Color("c29285")
            };
            _cardStyles[element] = style;
        }
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Stop;
    }

    /// <summary>提交完整 Tick 终态，瞬时反馈只依据实际快照变化。</summary>
    public void Submit(NpcSnapshot npc, int ticksPerRow, bool playing)
    {
        _used.Clear();
        _removed.Clear();
        if (_npc != null)
        {
            foreach (var card in npc.Cards)
            {
                var before = _npc.Cards.FirstOrDefault(c => c.Id == card.Id);
                if (before != null && card.UseCount > before.UseCount) _used[card.Id] = card.UseCount - before.UseCount;
            }
            _removed.AddRange(_npc.Backpack.Mana.Where(old => !npc.Backpack.Mana.Any(m => m.X == old.X && m.Y == old.Y && m.Element == old.Element)));
        }
        _ticksPerRow = ticksPerRow;
        _fromLine = _npc != null && npc.Backpack.Round == _npc.Backpack.Round ? _line : 0;
        _npc = npc;
        _targetLine = (npc.Backpack.Row + (float)(npc.Backpack.ScanProgress / ticksPerRow)) / npc.Backpack.Height;
        _elapsed = 0;
        _playing = playing;
        if (!playing) _line = _targetLine;
        QueueRedraw();
    }

    /// <summary>暂停立即对齐已提交位置并取消旧瞬态。</summary>
    public void SetPlaying(bool playing)
    {
        _playing = playing;
        if (!playing)
        {
            _line = _targetLine;
            _removed.Clear();
            _elapsed = 1;
        }
        QueueRedraw();
    }

    /// <summary>只推进客户端光效时间，绝不读取或推进模拟。</summary>
    public override void _Process(double delta)
    {
        if (!_playing || _npc == null) return;
        _elapsed += (float)delta;
        _line = Mathf.Lerp(_fromLine, _targetLine, Mathf.Clamp(_elapsed / 0.095f, 0, 1));
        QueueRedraw();
    }

    /// <summary>全部悬浮信息来自已提交的值快照。</summary>
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion motion || _npc == null) return;
        int x = (int)(motion.Position.X / (Size.X / _npc.Backpack.Width));
        int y = (int)(motion.Position.Y / (Size.Y / _npc.Backpack.Height));
        var card = _npc.Cards.FirstOrDefault(c => c.Cells.Any(cell => cell.X == x && cell.Y == y));
        if (card != null)
        {
            string resources = string.Join("、", card.RequiredElements.Select(e => $"{ObserverArtCatalog.ElementName(e.Element)} ×{e.Amount}"));
            TooltipText = $"{card.Name} [{card.DefineId}]\n实例 {card.Id} · 占格 {card.Size}\n状态 {card.Phase}{(card.IsFrozen ? " · 冻结" : "")}\n真实点亮 {card.AssignedElements.Count}/{card.Size} · 需求 {resources}\n累计使用 {card.UseCount} · 生效 {card.ApplyCount}";
        }
        else
        {
            var mana = _npc.Backpack.Mana.FirstOrDefault(m => m.X == x && m.Y == y);
            TooltipText = $"格 ({x + 1}, {y + 1})" + (mana == null ? "" : $"\n{ObserverArtCatalog.ElementName(mana.Element)}灵元 · 仍在原格");
        }
    }

    /// <summary>绘制真实格子、占格、资源和裁切扫描线。</summary>
    public override void _Draw()
    {
        if (_npc == null || _art == null) return;
        var pack = _npc.Backpack;
        float cw = Size.X / pack.Width, ch = Size.Y / pack.Height;
        var font = GetThemeDefaultFont();
        for (int y = 0; y < pack.Height; y++)
        for (int x = 0; x < pack.Width; x++)
        {
            var rect = new Rect2(x * cw + 2, y * ch + 2, cw - 4, ch - 4);
            bool valid = pack.ValidCells.Any(cell => cell.X == x && cell.Y == y);
            if (valid && _cellStyle != null) DrawStyleBox(_cellStyle, rect);
            else DrawRect(rect, valid ? new Color("d5cba8", 0.86f) : new Color("303c39", 0.7f));
            if (!valid) DrawLine(rect.Position, rect.End, new Color("6d7062"), 1);
        }
        foreach (var card in _npc.Cards)
        {
            if (card.Cells.Count == 0) continue;
            var rect = CardRect(card, cw, ch);
            string element = card.RequiredElements.FirstOrDefault()?.Element ?? "Jin";
            DrawStyleBox(_cardStyles.TryGetValue(card.IsWound ? "Wound" : element, out var style) ? style : _cardStyles["Jin"], rect);
            if (card.IsMain) DrawRect(rect.Grow(-1), new Color("f8df9a"), false, 2);
            var texture = _art.Card(card.DefineId);
            if (texture != null) DrawFit(texture, new Rect2(rect.Position + new Vector2(4, 17), new Vector2(rect.Size.X - 8, rect.Size.Y - 31)), Colors.White, _art.CardTranspose(card.DefineId));
            else DrawString(font, rect.Position + new Vector2(6, rect.Size.Y * 0.6f), "暂无图", HorizontalAlignment.Center, rect.Size.X - 12, 14, new Color("465650"));
            DrawString(font, rect.Position + new Vector2(3, 14), card.Name, HorizontalAlignment.Center, rect.Size.X - 6, 13, new Color("334336"));
            string state = card.IsMain ? "主功法" : card.IsWound ? "伤口" : $"亮 {card.AssignedElements.Count}/{card.Size}";
            if (card.IsFrozen) state = $"冻 {card.AssignedElements.Count}/{card.Size}";
            if (_used.TryGetValue(card.Id, out int use)) state = $"用 ×{use}";
            DrawString(font, rect.Position + new Vector2(2, rect.Size.Y - 3), state, HorizontalAlignment.Center, rect.Size.X - 4, 12, new Color("334336"));
            if (card.AssignedElements.Count > 0)
            {
                float px = rect.Position.X + 5;
                foreach (var assigned in card.AssignedElements)
                {
                    DrawCircle(new Vector2(px + 3, rect.End.Y - 18), 3, ObserverArtCatalog.ElementColor(assigned));
                    px += 10;
                }
                DrawRect(rect.Grow(-2), new Color("ffffc0", card.AssignedElements.Count == card.Size ? 0.9f : 0.4f), false, 2);
            }
            float scanY = _line * Size.Y;
            if (scanY >= rect.Position.Y && scanY <= rect.End.Y)
                DrawRect(rect.Grow(-2), new Color(0.67f, 0.91f, 1, 0.8f), false, 2);
            if (card.IsFrozen) DrawRect(rect.Grow(-1), new Color("91dbff"), false, 3);
            if (_used.ContainsKey(card.Id) && _playing && _elapsed < 0.1f)
                DrawRect(rect.Grow(-4), new Color(1, 0.95f, 0.6f, (1 - _elapsed / 0.1f) * 0.32f));
        }
        foreach (var mana in pack.Mana) DrawMana(mana, cw, ch, 1);
        if (_playing && _elapsed < 0.095f)
            foreach (var mana in _removed) DrawMana(mana, cw, ch, 1 - _elapsed / 0.095f);
        // 多层薄带形成柔光；光线只表示核心行时间，不是逐格触发器。
        float lineY = Mathf.Clamp(_line * Size.Y, 1, Size.Y - 1);
        for (int i = 7; i >= 1; i--)
            DrawRect(new Rect2(0, lineY - i * 1.7f, Size.X, i * 3.4f), new Color(0.2f, 0.66f, 1, 0.025f));
        DrawLine(new Vector2(0, lineY), new Vector2(Size.X, lineY), new Color("8bdbff"), 5);
        DrawLine(new Vector2(0, lineY), new Vector2(Size.X, lineY), new Color("f2feff"), 1.8f);
    }

    private Rect2 CardRect(CardSnapshot card, float cw, float ch)
    {
        int x = card.Cells.Min(c => c.X), y = card.Cells.Min(c => c.Y);
        return new Rect2(x * cw + 2, y * ch + 2, (card.Cells.Max(c => c.X) - x + 1) * cw - 4, (card.Cells.Max(c => c.Y) - y + 1) * ch - 4);
    }

    private void DrawMana(ManaSnapshot mana, float cw, float ch, float alpha)
    {
        var center = new Vector2((mana.X + 0.5f) * cw, (mana.Y + 0.5f) * ch);
        float distance = Mathf.Abs(center.Y - _line * Size.Y);
        float glow = Mathf.Clamp(1 - distance / (ch * 0.55f), 0, 1);
        var color = ObserverArtCatalog.ElementColor(mana.Element);
        if (glow > 0)
        {
            DrawCircle(center, Math.Min(cw, ch) * 0.38f, new Color(color, glow * 0.18f * alpha));
            DrawCircle(center, Math.Min(cw, ch) * 0.29f, new Color(0.8f, 0.96f, 1, glow * 0.21f * alpha));
        }
        var texture = _art!.Mana(mana.Element);
        var area = new Rect2(center - new Vector2(cw, ch) * 0.32f, new Vector2(cw, ch) * 0.64f);
        if (texture != null) DrawFit(texture, area, new Color(1 + glow * 0.4f, 1 + glow * 0.4f, 1 + glow * 0.4f, alpha));
        else
        {
            DrawCircle(center, Math.Min(cw, ch) * 0.25f, new Color(color, alpha));
            DrawString(GetThemeDefaultFont(), center + new Vector2(-cw * 0.3f, 6), ObserverArtCatalog.ElementName(mana.Element), HorizontalAlignment.Center, cw * 0.6f, 17, new Color("1c363e", alpha));
        }
    }

    private void DrawFit(Texture2D texture, Rect2 area, Color color, bool transpose = false)
    {
        var sourceSize = transpose ? new Vector2(texture.GetHeight(), texture.GetWidth()) : texture.GetSize();
        float scale = Mathf.Min(area.Size.X / sourceSize.X, area.Size.Y / sourceSize.Y);
        var size = sourceSize * scale;
        if (transpose)
        {
            // AtlasTexture 的 transpose 绘制会沿用裁切区域方向；显式旋转绘制坐标保持主体完整。
            var originalSize = texture.GetSize() * scale;
            DrawSetTransform(area.GetCenter(), -Mathf.Pi / 2);
            DrawTextureRect(texture, new Rect2(-originalSize / 2, originalSize), false, color);
            DrawSetTransform(Vector2.Zero);
        }
        else DrawTextureRect(texture, new Rect2(area.Position + (area.Size - size) / 2, size), false, color);
    }
}
