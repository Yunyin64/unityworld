using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>背包格与已提交显示态。飞行由独立效果层绘制，不比较快照猜事件。</summary>
public partial class ObserverBackpackView : Control
{
    private NpcSnapshot? _npc;
    private ObserverArtCatalog? _art;
    private StyleBoxFlat? _cellStyle;
    private float _fromLine, _targetLine, _line, _elapsed = 1;
    private int _scanRound = -1;
    private readonly Dictionary<int, float> _flashes = new();
    private readonly HashSet<int> _hidden = new();
    public float ScanLineFraction => _line;
    public int? NpcId => _npc?.Id;

    public void Configure(ObserverArtCatalog art, StyleBoxFlat? cellStyle = null)
    {
        _art = art;
        _cellStyle = cellStyle;
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Stop;
    }

    /// <summary>卡面消费局部表现状态；扫描时钟独立读取最新已提交快照，不等待动画。</summary>
    public void Submit(NpcSnapshot npc, int ticksPerRow, bool playing, BackpackSnapshot? liveScan = null)
    {
        var scan = liveScan ?? npc.Backpack;
        float next = (scan.Row + (float)(scan.ScanProgress / Math.Max(1, ticksPerRow))) / Math.Max(1, scan.Height);
        if (Math.Abs(next - _targetLine) > 0.00001f || scan.Round != _scanRound)
        {
            _fromLine = _scanRound == scan.Round ? _line : 0;
            _targetLine = next;
            _elapsed = 0;
        }
        _scanRound = scan.Round;
        _npc = npc;
        QueueRedraw();
    }

    /// <summary>暂停不取消短动画；扫描线同样收尾到本步位置。</summary>
    public void SetPlaying(bool playing) { }
    public void SetEffects(IEnumerable<KeyValuePair<int, float>> flashes, IEnumerable<int> hidden)
    {
        _flashes.Clear();
        foreach (var item in flashes) _flashes[item.Key] = item.Value;
        _hidden.Clear();
        foreach (int id in hidden) _hidden.Add(id);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_elapsed >= 0.095f || _npc == null) return;
        _elapsed += (float)delta;
        _line = Mathf.Lerp(_fromLine, _targetLine, Mathf.Clamp(_elapsed / 0.095f, 0, 1));
        QueueRedraw();
    }

    public Vector2 CellCenter(int x, int y) => new((x + 0.5f) * Size.X / (_npc?.Backpack.Width ?? 5), (y + 0.5f) * Size.Y / (_npc?.Backpack.Height ?? 5));
    public Rect2 CardBounds(IReadOnlyList<CellSnapshot> cells)
    {
        float cw = Size.X / (_npc?.Backpack.Width ?? 5), ch = Size.Y / (_npc?.Backpack.Height ?? 5);
        if (cells.Count == 0) return new Rect2(Size / 2 - new Vector2(cw, ch) / 2, new Vector2(cw, ch));
        int x = cells.Min(c => c.X), y = cells.Min(c => c.Y);
        return new Rect2(x * cw + 2, y * ch + 2, (cells.Max(c => c.X) - x + 1) * cw - 4, (cells.Max(c => c.Y) - y + 1) * ch - 4);
    }
    public IReadOnlyList<Rect2> CardCells(IReadOnlyList<CellSnapshot> cells)
    {
        float cw = Size.X / (_npc?.Backpack.Width ?? 5), ch = Size.Y / (_npc?.Backpack.Height ?? 5);
        return cells.Select(c => new Rect2(c.X * cw + 2, c.Y * ch + 2, cw - 4, ch - 4)).ToArray();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion motion || _npc == null) return;
        int x = (int)(motion.Position.X / (Size.X / _npc.Backpack.Width));
        int y = (int)(motion.Position.Y / (Size.Y / _npc.Backpack.Height));
        var card = _npc.Cards.FirstOrDefault(c => c.Cells.Any(cell => cell.X == x && cell.Y == y));
        if (card != null)
        {
            string resources = string.Join("、", card.RequiredElements.Select(e => $"{ObserverArtCatalog.ElementName(e.Element)} ×{e.Amount}"));
            TooltipText = $"{card.Name} [{card.DefineId}]\n实例 {card.Id} · 占格 {card.Size}\n状态 {card.Phase}{(card.IsFrozen ? " · 冻结" : "")}\n真实点亮 {card.AssignedElements.Count}/{card.RequiredElements.Sum(e => e.Amount)} · 需求 {resources}\n累计使用 {card.UseCount} · 生效 {card.ApplyCount}";
        }
        else
        {
            var mana = _npc.Backpack.Mana.FirstOrDefault(m => m.X == x && m.Y == y);
            TooltipText = $"格 ({x + 1}, {y + 1})" + (mana == null ? "" : $"\n{ObserverArtCatalog.ElementName(mana.Element)}灵元 · 仍在原格");
        }
    }

    public override void _Draw()
    {
        if (_npc == null || _art == null) return;
        var pack = _npc.Backpack;
        float cw = Size.X / pack.Width, ch = Size.Y / pack.Height;
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
            if (card.Cells.Count == 0 || _hidden.Contains(card.Id)) continue;
            ObserverCardPainter.Draw(this, _art, card, CardBounds(card.Cells), CardCells(card.Cells), flash: _flashes.GetValueOrDefault(card.Id));
        }
        foreach (var mana in pack.Mana) DrawMana(mana, cw, ch);
        float lineY = Mathf.Clamp(_line * Size.Y, 1, Size.Y - 1);
        for (int i = 7; i >= 1; i--)
            DrawRect(new Rect2(0, lineY - i * 1.7f, Size.X, i * 3.4f), new Color(0.2f, 0.66f, 1, 0.025f));
        DrawLine(new Vector2(0, lineY), new Vector2(Size.X, lineY), new Color("8bdbff"), 5);
        DrawLine(new Vector2(0, lineY), new Vector2(Size.X, lineY), new Color("f2feff"), 1.8f);
    }

    private void DrawMana(ManaSnapshot mana, float cw, float ch)
    {
        var center = CellCenter(mana.X, mana.Y);
        float glow = Mathf.Clamp(1 - Mathf.Abs(center.Y - _line * Size.Y) / (ch * 0.55f), 0, 1);
        var color = ObserverArtCatalog.ElementColor(mana.Element);
        if (glow > 0) DrawCircle(center, Math.Min(cw, ch) * 0.38f, new Color(color, glow * 0.25f));
        var texture = _art!.Mana(mana.Element);
        var area = new Rect2(center - new Vector2(cw, ch) * 0.32f, new Vector2(cw, ch) * 0.64f);
        if (texture != null) DrawTextureRect(texture, area, false, new Color(1 + glow * 0.4f, 1 + glow * 0.4f, 1 + glow * 0.4f));
        else
        {
            DrawCircle(center, Math.Min(cw, ch) * 0.25f, color);
            DrawString(GetThemeDefaultFont(), center + new Vector2(-cw * 0.3f, 6), ObserverArtCatalog.ElementName(mana.Element), HorizontalAlignment.Center, cw * 0.6f, 17, new Color("1c363e"));
        }
    }
}
