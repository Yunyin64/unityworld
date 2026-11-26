using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Game.Domain.Combat;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>只用显示时钟和事实值绘制瞬态。无 Tween 回调，取消后不会访问旧节点或回写状态。</summary>
public partial class ObserverPresentationLayer : Control
{
    private ObserverBattleLayout? _layout;
    private IReadOnlyList<PresentationCue> _cues = Array.Empty<PresentationCue>();
    private IReadOnlyList<NpcSnapshot> _participants = Array.Empty<NpcSnapshot>();
    private readonly Dictionary<(int Generation, long Sequence), int> _textLanes = new();

    internal void Configure(ObserverBattleLayout layout)
    {
        _layout = layout;
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
            if (_layout.Pending.Card(i) is { } pending)
                hidden = hidden.Append(pending.CardId);
            _layout.Packs[i].SetEffects(flashes, hidden);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_layout == null) return;
        float backpackOpacity = _layout.Stage.BackpackOpacity;
        if (backpackOpacity > .01f)
        {
            foreach (var cue in _cues)
            {
                if (cue.Event.ManaAssigned != null) DrawMana(cue, backpackOpacity);
                if (cue.Event.CardAdded != null) DrawAddedCard(cue, backpackOpacity);
            }
        }
        for (int side = 0; side < _layout.Packs.Length; side++)
        {
            if (_layout.Pending.Flight(side) is { } flight && _layout.Pending.Card(side) is { } prepared)
                DrawPreparedFlight(side, flight, prepared);
        }
        // 同一击产生入包卡片时，结算数值始终在飞入的卡片上方。
        foreach (var cue in _cues)
            if (cue.Event.Applied != null) DrawApplied(cue);
        foreach (var cue in _cues)
            if (cue.Event.Damage != null) DrawDamage(cue);
    }

    private void DrawPreparedFlight(int side, PresentationCue cue, PendingSnapshot prepared)
    {
        var layout = _layout!;
        var pack = layout.Packs[side];
        var stage = layout.Stage;
        var toLayer = GetGlobalTransform().AffineInverse();
        var cells = cue.Event.Card!.Cells.Select(cell => new CellSnapshot(cell.X, cell.Y)).ToArray();
        var from = (toLayer * pack.GetGlobalTransform()) * pack.CardBounds(cells);
        var to = (toLayer * stage.GetGlobalTransform()) * stage.ReadyCardBounds(side);
        float progress = Ease(cue.Progress);
        var center = from.GetCenter().Lerp(to.GetCenter(), progress);
        center.Y -= MathF.Sin(progress * Mathf.Pi) * Math.Min(72, from.GetCenter().DistanceTo(to.GetCenter()) * .21f);
        var size = from.Size.Lerp(to.Size, progress);
        stage.DrawPreparedCard(this, prepared, new Rect2(center - size / 2, size), 1);
    }

    private void DrawMana(PresentationCue cue, float opacity)
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
        DrawCircle(start, 12 + p * 12, new Color(color, Math.Max(0, 1 - p * 4) * 0.65f * opacity));
        DrawCircle(at, 18, new Color(color, 0.16f * opacity));
        DrawLine(at, at.Lerp(start, 0.14f), new Color(color, 0.45f * opacity), 4);
        var texture = _layout!.Art.Mana(mana.Element);
        float size = p < 0.88f ? 30 : Mathf.Lerp(30, 14, (p - 0.88f) / 0.12f);
        if (texture != null) DrawTextureRect(texture, new Rect2(at - Vector2.One * size / 2, Vector2.One * size), false, new Color(Colors.White, opacity));
        else DrawCircle(at, size / 3, new Color(color, opacity));
        if (p > 0.8f) DrawArc(end, 12 + (p - 0.8f) * 70, 0, Mathf.Tau, 24, new Color(color, (1 - p) * 4 * opacity), 2);
    }

    private void DrawAddedCard(PresentationCue cue, float opacity)
    {
        var fact = cue.Event;
        var pack = Pack(fact.NpcId);
        int side = Side(fact.NpcId);
        if (pack == null || fact.Card == null || side < 0) return;
        var card = _participants.SelectMany(n => n.Cards).FirstOrDefault(c => c.Id == fact.Card.Id)
            ?? cue.AfterSnapshot.Participants.SelectMany(n => n.Cards).FirstOrDefault(c => c.Id == fact.Card.Id);
        if (card == null) return;
        var packToLayer = GetGlobalTransform().AffineInverse() * pack.GetGlobalTransform();
        var targetBounds = packToLayer * pack.CardBounds(card.Cells);
        var targetCenter = targetBounds.GetCenter();
        var stage = _layout!.Stage;
        // 入包反馈从靠近储物阵的下方离开，命中数字留在人身边。
        float lane = ((int)(fact.Sequence % 3) - 1) * 31;
        bool fromStage = stage.IsFighting;
        var start = fromStage
            ? From(stage, stage.FootAt(side) + new Vector2((side == 0 ? -1 : 1) * 54 + lane, 20))
            : From(stage, stage.HeadAt(side)) + new Vector2(lane, 68);
        float p = (float)cue.Progress;
        float travel = Ease(Mathf.Clamp((p - 0.23f) / 0.65f, 0, 1));
        var center = start.Lerp(targetCenter, travel) + new Vector2(0, -MathF.Sin(travel * MathF.PI) * (fromStage ? 10 : 55));
        float scale = Mathf.Lerp(fromStage ? .69f : 1.45f, 1, travel);
        float alpha = Mathf.Clamp(p / 0.12f, 0, 1) * opacity;
        var bounds = new Rect2(center - targetBounds.Size * scale / 2, targetBounds.Size * scale);
        // 入包时也换算实际格子的大小；设计者缩放整块背包后，飞行卡与最终卡面连续衔接。
        var cells = pack.CardCells(card.Cells).Select(c => packToLayer * c)
            .Select(c => new Rect2(center + (c.Position - targetCenter) * scale, c.Size * scale)).ToArray();
        ObserverCardPainter.Draw(this, _layout.Art, card, bounds, cells, alpha, p > 0.88f ? (1 - p) / 0.12f : 0);
    }

    private void DrawDamage(PresentationCue cue)
    {
        var damage = cue.Event.Damage!;
        if (damage.HpDamage <= 0 && damage.ShieldAbsorbed <= 0) return;
        int side = Side(damage.TargetNpcId);
        if (side < 0) return;
        float p = (float)cue.Progress;
        bool direct = BattlePresentationPlayer.IsDirectStrike(cue.Event);
        if (direct && p < BattlePresentationPlayer.StrikeContact) return;
        if (direct) p = (p - BattlePresentationPlayer.StrikeContact) / (1 - BattlePresentationPlayer.StrikeContact);
        var stage = _layout!.Stage;
        if (!direct && stage.HoldsDamage(cue.Event)) p = .027f;
        bool heavy = ObserverDuelStage.HeavyHit(damage);
        float pop = p < .18f ? 1 + MathF.Sin(p / .18f * MathF.PI) * (heavy ? .42f : .29f) : 1;
        int lane = _textLanes.GetValueOrDefault((cue.Generation, cue.Event.Sequence));
        // 命中数字跟随当前受击位置。回场时降低透明度，避免数字跟着人物突然跳回。
        float fade = (direct ? Math.Max(.83f * (1 - p), Fade(p)) : Fade(p)) *
            (direct ? 1 - Ease(Mathf.Clamp((cue.Progress - .78f) / (ObserverDuelStage.ReturnJump - .78f), 0, 1))
                : stage.IsReturning ? .44f : 1);
        float spread = stage.IsFighting ? (side == 0 ? -1 : 1) * (stage.HeightAt(side) * .43f + 18 * p) : -55;
        // 拼点牌在角色肩线上方。伤害字从外侧胸口往上飘，避开脸和卡面。
        float baseline = stage.IsFighting ? stage.HeightAt(side) * .42f : 32;
        var at = stage.HeadAt(side) + new Vector2(lane % 2 == 0 ? spread : -spread, baseline + (lane / 2) * 54 - p * 27);
        if (damage.HpDamage > 0)
            StageText($"−{damage.HpDamage}", at, (int)((heavy ? 46 : 37) * pop), new Color(heavy ? "ffd2aa" : "ffad98"), fade, heavy ? 8 : 6);
        if (damage.ShieldAbsorbed > 0)
        {
            float y = damage.HpDamage > 0 ? 23 : 0;
            StageText($"护盾 −{damage.ShieldAbsorbed}", at + new Vector2(0, y), 21, new Color("b4ebff"), fade, 4);
        }
    }

    private void StageText(string text, Vector2 at, int size, Color color, float alpha, int outline)
    {
        var stage = _layout!.Stage;
        var limits = stage.TextLimits;
        float width = GetThemeDefaultFont().GetStringSize(text, fontSize: size).X;
        float maxWidth = (limits.Y - limits.X) * .46f;
        if (width > maxWidth)
        {
            size = Math.Max(12, (int)(size * maxWidth / width));
            width = GetThemeDefaultFont().GetStringSize(text, fontSize: size).X;
        }
        float margin = Math.Min(width / 2 + outline, (limits.Y - limits.X) / 2);
        at.X = Mathf.Clamp(at.X, limits.X + margin, limits.Y - margin);
        FloatingText(text, From(stage, at), size, color, alpha, outline);
    }

    private void DrawApplied(PresentationCue cue)
    {
        if (cue.Event.Card == null) return;
        int side = Side(cue.Event.NpcId);
        if (side < 0) return;
        float p = (float)cue.Progress;
        int lane = _textLanes.GetValueOrDefault((cue.Generation, cue.Event.Sequence));
        var stage = _layout!.Stage;
        float textY = stage.IsFighting ? 27 : -18;
        if (_layout!.Art.CastElement(cue.Event) is { } element)
        {
            if (p < ObserverDuelStage.CastRelease) return;
            p = (p - ObserverDuelStage.CastRelease) / (1 - ObserverDuelStage.CastRelease);
            var actual = stage.HeadAt(side) + new Vector2(0, textY - lane * 24 - p * 11);
            StageText(ObserverArtCatalog.CardName(cue.Event.Card.Name, cue.Event.Card.DefineId) + " · 生效",
                actual, 21, ObserverArtCatalog.ElementColor(element).Lerp(Colors.White, .43f), Fade(p), 3);
            return;
        }
        if (stage.IsFighting)
        {
            var limits = stage.TextLimits;
            var foot = stage.FootAt(side);
            float x = Mathf.Lerp(limits.X, limits.Y, side == 0 ? .24f : .76f);
            StageText(ObserverArtCatalog.CardName(cue.Event.Card.Name, cue.Event.Card.DefineId) + " · 生效",
                new Vector2(x, foot.Y + 24 + lane * 19), 16, new Color("e2ce9c"), Fade(p) * .82f, 3);
            return;
        }
        var at = stage.HeadAt(side) + new Vector2(0, textY - lane * 24 - p * 10);
        StageText(ObserverArtCatalog.CardName(cue.Event.Card.Name, cue.Event.Card.DefineId) + " · 生效",
            at, 21, new Color("fff0b4"), Fade(p), 3);
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
    private Vector2 From(CanvasItem control, Vector2 local) => GetGlobalTransform().AffineInverse() * (control.GetGlobalTransform() * local);
    private static float Ease(float p) => p * p * (3 - 2 * p);
    private static float Fade(float p) => Mathf.Clamp(p / 0.06f, 0, 1) * Mathf.Clamp((1 - p) / 0.27f, 0, 1);
}
