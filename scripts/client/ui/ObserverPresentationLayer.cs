using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Game.Domain.Combat;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>只用显示时钟和事实值绘制瞬态。无 Tween 回调，取消后不会访问旧节点或回写状态。</summary>
internal partial class ObserverPresentationLayer : Control
{
    private ObserverBattleLayout? _layout;
    private IReadOnlyList<PresentationCue> _cues = Array.Empty<PresentationCue>();
    private IReadOnlyList<NpcSnapshot> _participants = Array.Empty<NpcSnapshot>();
    private readonly Dictionary<(int Generation, long Sequence), int> _textLanes = new();

    public void Configure(ObserverBattleLayout layout)
    {
        _layout = layout;
        Size = new Vector2(1600, 900);
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 5;
    }

    public void Submit(IReadOnlyList<NpcSnapshot> participants, IReadOnlyList<PresentationCue> cues)
    {
        _participants = participants;
        _cues = cues;
        AssignTextLanes(cues);
        for (int i = 0; i < _layout!.Packs.Length; i++)
        {
            int? id = _layout.Packs[i].NpcId;
            var owned = cues.Where(c => c.Event.NpcId == id);
            var flashes = owned.Where(c => c.Event.CardUsed != null && c.Event.Card != null)
                .Select(c => new KeyValuePair<int, float>(c.Event.Card!.Id, MathF.Sin((float)c.Progress * MathF.PI)));
            var hidden = owned.Where(c => c.Event.CardAdded != null && c.Event.Card != null).Select(c => c.Event.Card!.Id);
            _layout.Packs[i].SetEffects(flashes, hidden);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_layout == null) return;
        foreach (var cue in _cues)
        {
            if (cue.Event.ManaAssigned != null) DrawMana(cue);
            if (cue.Event.CardAdded != null) DrawAddedCard(cue);
            if (cue.Event.Damage != null) DrawDamage(cue);
            if (cue.Event.Applied != null) DrawApplied(cue);
        }
        float flash = _layout.Pending.CollisionFlash;
        if (flash > 0)
        {
            var at = _layout.Pending.CollisionAnchor;
            DrawCircle(at, 26 + 16 * (1 - flash), new Color(1, 0.94f, 0.67f, flash * 0.22f));
            DrawLine(at - new Vector2(0, 34), at + new Vector2(0, 34), new Color(1, 1, 0.9f, flash), 4);
            DrawLine(at - new Vector2(21, 20), at + new Vector2(21, 20), new Color(1, 0.94f, 0.7f, flash), 3);
            DrawLine(at - new Vector2(21, -20), at + new Vector2(21, -20), new Color(1, 0.94f, 0.7f, flash), 3);
        }
    }

    private void DrawMana(PresentationCue cue)
    {
        var fact = cue.Event;
        var mana = fact.ManaAssigned!;
        var pack = Pack(fact.NpcId);
        if (pack == null || fact.Card == null) return;
        float p = (float)cue.Progress;
        var cells = fact.Card.Cells.Select(c => new CellSnapshot(c.X, c.Y)).ToArray();
        var start = From(pack, pack.CellCenter(mana.SourceCell.X, mana.SourceCell.Y));
        var end = From(pack, pack.CardBounds(cells).GetCenter());
        float t = Ease(Mathf.Clamp((p - 0.12f) / 0.78f, 0, 1));
        var at = start.Lerp(end, t) + new Vector2(0, -MathF.Sin(t * MathF.PI) * Math.Min(36, start.DistanceTo(end) * 0.2f));
        var color = ObserverArtCatalog.ElementColor(mana.Element);
        DrawCircle(start, 12 + p * 12, new Color(color, Math.Max(0, 1 - p * 4) * 0.65f));
        DrawCircle(at, 18, new Color(color, 0.16f));
        DrawLine(at, at.Lerp(start, 0.14f), new Color(color, 0.45f), 4);
        var texture = _layout!.Art.Mana(mana.Element);
        float size = p < 0.88f ? 30 : Mathf.Lerp(30, 14, (p - 0.88f) / 0.12f);
        if (texture != null) DrawTextureRect(texture, new Rect2(at - Vector2.One * size / 2, Vector2.One * size), false);
        else DrawCircle(at, size / 3, color);
        if (p > 0.8f) DrawArc(end, 12 + (p - 0.8f) * 70, 0, Mathf.Tau, 24, new Color(color, (1 - p) * 4), 2);
    }

    private void DrawAddedCard(PresentationCue cue)
    {
        var fact = cue.Event;
        var pack = Pack(fact.NpcId);
        int side = Side(fact.NpcId);
        if (pack == null || fact.Card == null || side < 0) return;
        var card = _participants.SelectMany(n => n.Cards).FirstOrDefault(c => c.Id == fact.Card.Id)
            ?? cue.AfterSnapshot.Participants.SelectMany(n => n.Cards).FirstOrDefault(c => c.Id == fact.Card.Id);
        if (card == null) return;
        var targetBounds = pack.CardBounds(card.Cells);
        var targetCenter = From(pack, targetBounds.GetCenter());
        var start = _layout!.Actors[side].HeadAnchor + new Vector2(((int)(fact.Sequence % 3) - 1) * 38, 68);
        float p = (float)cue.Progress;
        float travel = Ease(Mathf.Clamp((p - 0.23f) / 0.65f, 0, 1));
        var center = start.Lerp(targetCenter, travel) + new Vector2(0, -MathF.Sin(travel * MathF.PI) * 55);
        float scale = Mathf.Lerp(1.45f, 1, travel);
        float alpha = Mathf.Clamp(p / 0.12f, 0, 1);
        var bounds = new Rect2(center - targetBounds.Size * scale / 2, targetBounds.Size * scale);
        var cells = pack.CardCells(card.Cells).Select(c => new Rect2(center + (c.Position - targetBounds.GetCenter()) * scale, c.Size * scale)).ToArray();
        ObserverCardPainter.Draw(this, _layout.Art, card, bounds, cells, alpha, p > 0.88f ? (1 - p) / 0.12f : 0);
    }

    private void DrawDamage(PresentationCue cue)
    {
        var damage = cue.Event.Damage!;
        if (damage.HpDamage <= 0 && damage.ShieldAbsorbed <= 0) return;
        int side = Side(damage.TargetNpcId);
        if (side < 0) return;
        string text = damage.HpDamage > 0 ? damage.HpDamage.ToString() : "吸收";
        float p = (float)cue.Progress;
        float pop = p < 0.16f ? 1 + MathF.Sin(p / 0.16f * MathF.PI) * 0.3f : 1;
        int lane = _textLanes.GetValueOrDefault((cue.Generation, cue.Event.Sequence));
        // 两列留足描边与放大宽度，避免并发的 20 和 12 视觉连成 2012。
        var at = _layout!.Actors[side].HeadAnchor + new Vector2(lane % 2 == 0 ? -50 : 50, 22 + (lane / 2) * 38 - p * 21);
        var color = damage.HpDamage > 0 ? new Color("ff8778") : new Color("9de7ff");
        FloatingText(text, at, (int)(34 * pop), color, Fade(p), 4);
    }

    private void DrawApplied(PresentationCue cue)
    {
        if (cue.Event.Card == null) return;
        int side = Side(cue.Event.NpcId);
        if (side < 0) return;
        float p = (float)cue.Progress;
        int lane = _textLanes.GetValueOrDefault((cue.Generation, cue.Event.Sequence));
        var at = _layout!.Actors[side].HeadAnchor + new Vector2(0, -25 - lane * 24 - p * 10);
        FloatingText(cue.Event.Card.Name + " · 生效", at, 21, new Color("fff0b4"), Fade(p), 3);
    }

    private void FloatingText(string text, Vector2 at, int size, Color color, float alpha, int outline)
    {
        var font = GetThemeDefaultFont();
        float width = font.GetStringSize(text, fontSize: size).X;
        var pos = at - new Vector2(width / 2, 0);
        DrawStringOutline(font, pos, text, HorizontalAlignment.Left, -1, size, outline, new Color("25353d", alpha));
        DrawString(font, pos, text, HorizontalAlignment.Left, -1, size, new Color(color, alpha));
    }

    /// <summary>同人物提示同时播放，存活期间固定槽位，不因旧提示淡出而跳动。</summary>
    private void AssignTextLanes(IReadOnlyList<PresentationCue> cues)
    {
        var text = cues.Where(c => c.Event.Applied != null || c.Event.Damage != null).ToArray();
        var alive = text.Select(c => (c.Generation, c.Event.Sequence)).ToHashSet();
        foreach (var key in _textLanes.Keys.Where(k => !alive.Contains(k)).ToArray()) _textLanes.Remove(key);
        foreach (var group in text.GroupBy(c => (Npc: c.Event.Damage?.TargetNpcId ?? c.Event.NpcId, Damage: c.Event.Damage != null)))
        {
            var used = group.Where(c => _textLanes.ContainsKey((c.Generation, c.Event.Sequence)))
                .Select(c => _textLanes[(c.Generation, c.Event.Sequence)]).ToHashSet();
            foreach (var cue in group)
            {
                var key = (cue.Generation, cue.Event.Sequence);
                if (_textLanes.ContainsKey(key)) continue;
                int lane = 0;
                while (used.Contains(lane)) lane++;
                _textLanes[key] = lane; used.Add(lane);
            }
        }
    }

    private ObserverBackpackView? Pack(int npcId) => _layout!.Packs.FirstOrDefault(p => p.NpcId == npcId);
    private int Side(int npcId)
    {
        for (int i = 0; i < Math.Min(2, _participants.Count); i++) if (_participants[i].Id == npcId) return i;
        return -1;
    }
    // 所有落点从控件转换到统一效果画布；窗口缩放不改变零基格坐标。
    private Vector2 From(Control control, Vector2 local) => GetGlobalTransform().AffineInverse() * (control.GetGlobalTransform() * local);
    private static float Ease(float p) => p * p * (3 - 2 * p);
    private static float Fade(float p) => Mathf.Clamp(p / 0.06f, 0, 1) * Mathf.Clamp((1 - p) / 0.27f, 0, 1);
}
