using Godot;
using System.Collections.Generic;
using System.Linq;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>绑定场景中的人物、属性与状态控件，只消费已提交的显示快照。</summary>
internal sealed class ObserverActorPanel
{
    private readonly Label _name, _actorName, _status;
    private readonly Control _actor;
    private readonly float _originalTop, _buffTop, _statusTop;
    public float PreferredLeft { get; }
    public Vector2 ActorSize { get; }
    private readonly HBoxContainer _buffList;
    private readonly ScrollContainer _buffScroll;
    private readonly Label[] _values;
    // 部分真实战斗修正没有实例 ID，同一 ID 也可能出现多条；序号只用于本页命中，不写入快照。
    private readonly record struct BuffKey(string? Id, string? Definition, int Ordinal);
    private readonly Dictionary<BuffKey, (Label Label, BuffSnapshot Buff)> _buffRows = new();
    private IReadOnlyList<BuffSnapshot> _buffs = System.Array.Empty<BuffSnapshot>();
    public Marker2D HeadAnchor { get; }

    public ObserverActorPanel(Control actor, Control stats)
    {
        _actor = actor;
        PreferredLeft = actor.OffsetLeft;
        _originalTop = actor.OffsetTop;
        ActorSize = actor.Size;
        _name = stats.GetNode<Label>("Name");
        _actorName = actor.GetNode<Label>("Name");
        HeadAnchor = actor.GetNode<Marker2D>("HeadAnchor");
        _status = actor.GetNode<Label>("Status");
        _buffScroll = actor.GetNode<ScrollContainer>("BuffScroll");
        _buffList = _buffScroll.GetNode<HBoxContainer>("Buffs");
        _buffTop = _buffScroll.Position.Y;
        _statusTop = _status.Position.Y;
        _values = new[] { "Hp", "Mp", "Sp", "Shield" }.Select(field => stats.GetNode<Label>($"{field}/Value")).ToArray();
        _name.Text = _actorName.Text = _status.Text = "";
        foreach (var label in _values) label.Text = "—";
        _buffScroll.Visible = false;
    }

    /// <summary>保留人物本身大小，只在两列背包挤近时挪动人物和状态条。</summary>
    public void Arrange(float left, float? top, bool staggerStatus)
    {
        _actor.OffsetLeft = left;
        _actor.OffsetRight = left + ActorSize.X;
        _actor.AnchorTop = _actor.AnchorBottom = top.HasValue ? 0 : 1;
        _actor.OffsetTop = top ?? _originalTop;
        _actor.OffsetBottom = _actor.OffsetTop + ActorSize.Y;
        _buffScroll.Position = new Vector2(_buffScroll.Position.X, _buffTop + (staggerStatus ? 40 : 0));
        _status.Position = new Vector2(_status.Position.X, _statusTop + (staggerStatus ? 40 : 0));
    }

    public void Submit(NpcSnapshot npc)
    {
        string name = string.IsNullOrWhiteSpace(npc.Name) ? $"角色 {npc.Id}" : npc.Name;
        _name.Text = _actorName.Text = name;
        _name.TooltipText = _actorName.TooltipText = name;
        _values[0].Text = Amount(npc.Hp, npc.HpMax);
        _values[1].Text = Amount(npc.Mp, npc.MpMax);
        _values[2].Text = Amount(npc.Sp, npc.SpMax);
        _values[3].Text = npc.Shield.ToString();
        foreach (var label in _values) label.TooltipText = npc.StatReadError ?? "当前值 / 当前上限；SP 为占用 / 角色上限，护盾无虚构上限。";
        _status.Text = npc.Buffs.Count != 0 || npc.Status == "Active" ? "" : npc.Status == "Defeated" ? "已败退" : npc.Status;
        _status.Visible = _status.Text.Length != 0;

        // 保留每个实例的命中控件，暂停时光标不动也能在状态移除当帧关闭详情。
        if (ReferenceEquals(_buffs, npc.Buffs) || _buffs.SequenceEqual(npc.Buffs)) return;
        _buffs = npc.Buffs;
        var identities = new Dictionary<(string? Id, string? Definition), int>();
        var current = new Dictionary<BuffKey, BuffSnapshot>();
        foreach (var buff in _buffs)
        {
            var identity = (buff.Id, buff.DefineId);
            int ordinal = identities.GetValueOrDefault(identity);
            identities[identity] = ordinal + 1;
            current.Add(new BuffKey(buff.Id, buff.DefineId, ordinal), buff);
        }
        // 重复标识减少时无法证明原先哪一个仍在。清掉这组旧命中目标，再按新快照逐条显示。
        var shortened = _buffRows.Keys.Where(k => k.Ordinal > 0 && !current.ContainsKey(k))
            .Select(k => (k.Id, k.Definition)).ToHashSet();
        foreach (var key in _buffRows.Keys.Where(k => !current.ContainsKey(k) || shortened.Contains((k.Id, k.Definition))).ToArray())
        {
            var label = _buffRows[key].Label;
            DetailTips.Hide(label);
            _buffList.RemoveChild(label);
            label.QueueFree();
            _buffRows.Remove(key);
        }
        _buffScroll.Visible = _buffs.Count != 0;
        int index = 0;
        foreach (var (key, buff) in current)
        {
            if (!_buffRows.TryGetValue(key, out var row))
            {
                var label = new Label { MouseFilter = Control.MouseFilterEnum.Stop, VerticalAlignment = VerticalAlignment.Center };
                label.AddThemeFontSizeOverride("font_size", 15);
                row = (label, buff);
                label.MouseEntered += () => ShowBuff(key);
                label.MouseExited += () => DetailTips.Hide(label);
                _buffList.AddChild(label);
            }
            _buffRows[key] = (row.Label, buff);
            _buffList.MoveChild(row.Label, index++);
            row.Label.Text = $"{BuffName(buff)}×{buff.Stacks}";
            if (row.Label.GetViewport().GuiGetHoveredControl() == row.Label) ShowBuff(key);
        }
    }

    private void ShowBuff(BuffKey key)
    {
        if (!_buffRows.TryGetValue(key, out var row)) return;
        var buff = row.Buff;
        string duration = buff.Duration > 0 ? $"持续 {buff.Duration:0.##} Tick · 剩余 {buff.RemainingTime:0.##} Tick" : "无持续时间";
        string identity = string.IsNullOrWhiteSpace(buff.Id) ? "实例标识未提供" : $"实例 {buff.Id}";
        string definition = string.IsNullOrWhiteSpace(buff.DefineId) ? "定义未提供" : $"定义 {buff.DefineId}";
        DetailTips.ShowBuff(row.Label, key.Ordinal.ToString(), BuffName(buff),
            $"{definition} · {identity}\n层数 {buff.Stacks}\n{duration}\n过期规则 {buff.ExpirePolicy}");
    }

    private static string BuffName(BuffSnapshot buff) => !string.IsNullOrWhiteSpace(buff.Name) ? buff.Name
        : !string.IsNullOrWhiteSpace(buff.DefineId) ? buff.DefineId : "状态名称缺失";
    private static string Amount(int value, int? maximum) => maximum.HasValue ? $"{value}/{maximum.Value}" : $"{value}/?";
}
