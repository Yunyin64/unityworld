using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Game.Domain.Combat;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>只有两个原待发槽内容容器运动；底框常驻，不产生第三组卡牌。</summary>
internal sealed class ObserverPendingPanel
{
    private readonly Control[] _content = new Control[2];
    private readonly TextureRect[] _images = new TextureRect[2];
    private readonly Label[] _names = new Label[2], _details = new Label[2], _empty = new Label[2];
    private readonly Vector2[] _origins = new Vector2[2];
    private readonly Marker2D[] _contacts = new Marker2D[2];
    private readonly Control _canvas;
    private readonly Label _title, _value, _status;
    private readonly ObserverArtCatalog _art;
    public Marker2D CollisionAnchor { get; }
    public float CollisionFlash { get; private set; }
    // 验收夹具记录设计画布坐标，不将窗口缩放计入卡槽运动。
    public IReadOnlyList<Vector2> ContentPositions => _content
        .Select(c => _canvas.GetGlobalTransform().AffineInverse() * c.GlobalPosition).ToArray();

    public ObserverPendingPanel(Control panel, Control canvas, ObserverArtCatalog art)
    {
        _art = art;
        _canvas = canvas;
        CollisionAnchor = panel.GetNode<Marker2D>("CollisionAnchor");
        for (int i = 0; i < 2; i++)
        {
            var slot = panel.GetNode<Control>(i == 0 ? "Left" : "Right");
            _empty[i] = slot.GetNode<Label>("Empty");
            var card = _content[i] = slot.GetNode<Control>("Content");
            _origins[i] = card.Position;
            _contacts[i] = card.GetNode<Marker2D>("Contact");
            _images[i] = card.GetNode<TextureRect>("Art");
            _names[i] = card.GetNode<Label>("Name");
            _details[i] = card.GetNode<Label>("Details");
            card.Visible = false;
        }
        _title = panel.GetNode<Label>("Title");
        _value = panel.GetNode<Label>("Value");
        _status = panel.GetNode<Label>("Status");
    }

    public void Submit(IReadOnlyList<NpcSnapshot> participants, IReadOnlyList<PresentationCue> cues)
    {
        CollisionFlash = 0;
        var pending = participants.Take(2).Select(p => p.Pending).ToArray();
        var contestCue = cues.FirstOrDefault(c => c.Event.Contest != null);
        if (contestCue?.Event.Contest is { } contest)
        {
            // 终态已空槽时仍在这两个现有容器里重现事实中的卡，不额外复制节点。
            foreach (var side in new[] { contest.Left, contest.Right })
            {
                int index = IndexOf(participants, side.NpcId);
                if (index >= 0) pending[index] = new PendingSnapshot(side.Card.Id, side.Card.DefineId, side.Card.Name, side.ContestType, side.Value, side.Element);
            }
        }
        for (int i = 0; i < 2; i++)
        {
            var item = i < pending.Length ? pending[i] : null;
            _content[i].Position = _origins[i];
            _content[i].Modulate = Colors.White;
            _content[i].Visible = item != null;
            _empty[i].Visible = item == null;
            _images[i].Texture = item == null ? null : _art.Card(item.DefineId);
            _names[i].Text = item == null ? "" : ObserverArtCatalog.CardName(item.Name, item.DefineId);
            _names[i].TooltipText = _names[i].Text;
            _details[i].Text = item == null ? "" : $"{ContestName(item.ContestType)} · {ObserverArtCatalog.ElementName(item.Element)}";
            if (contestCue?.Event.Contest is not { } active || item == null) continue;
            float p = (float)contestCue.Progress;
            float approach = p < 0.46f ? Ease(p / 0.46f) : p < 0.61f ? 1 : 1 - Ease((p - 0.61f) / 0.39f);
            float shake = p >= 0.46f && p < 0.75f ? MathF.Sin(p * 110) * 2.8f * (1 - p) : 0;
            // 碰撞距离取槽内接触点和中央标记；拖动、缩放待发区域后仍得到同一落点。
            var slot = _content[i].GetParent<Control>();
            var toSlot = slot.GetGlobalTransform().AffineInverse();
            var travel = toSlot * CollisionAnchor.GlobalPosition - toSlot * _contacts[i].GlobalPosition;
            _content[i].Position += travel * approach + new Vector2(0, shake);
            if (p >= 0.46f && p <= 0.7f)
            {
                CollisionFlash = 1 - (p - 0.46f) / 0.24f;
                if (i < participants.Count && active.WinnerNpcId == participants[i].Id)
                    _content[i].Modulate = new Color(1.2f, 1.14f, 0.9f);
            }
        }
        _value.Text = string.Join(" : ", pending.Select(p => p?.Value.ToString() ?? "—"));
        _status.Text = contestCue?.Event.Contest is { } result ? (result.WinnerNpcId.HasValue ? "对拼" : "平局")
            : pending.All(p => p == null) ? "等待出招" : pending.All(p => p != null) ? "等待对拼" : "等待对手";
        // 对撞时给移动卡面留出中央区域；收尾后由当前状态恢复，避免文字压住卡图。
        _title.Visible = _value.Visible = _status.Visible = contestCue == null;
    }

    private static int IndexOf(IReadOnlyList<NpcSnapshot> participants, int id)
    {
        for (int i = 0; i < Math.Min(2, participants.Count); i++) if (participants[i].Id == id) return i;
        return -1;
    }
    private static float Ease(float p) { p = Mathf.Clamp(p, 0, 1); return p * p * (3 - 2 * p); }
    private static string ContestName(string kind) => kind switch
    {
        "Zhan" => "斩击", "Da" => "打击", "Ci" => "刺击", "SheJi" => "射击", "Shield" => "护盾", "Block" => "格挡", "Dodge" => "闪避", _ => kind
    };
}
