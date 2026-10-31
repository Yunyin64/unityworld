using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Game.Domain.Combat;
using Xianxia.Client.Battle;
using static Xianxia.Client.UI.ObserverWidgets;

namespace Xianxia.Client.UI;

/// <summary>只有两个原待发槽内容容器运动；底框常驻，不产生第三组卡牌。</summary>
internal sealed class ObserverPendingPanel
{
    private readonly Control[] _content = new Control[2];
    private readonly TextureRect[] _images = new TextureRect[2];
    private readonly Label[] _names = new Label[2], _details = new Label[2], _empty = new Label[2];
    private readonly Vector2[] _origins = { new(592, 200), new(858, 200) };
    private readonly Label _title, _value, _status;
    private readonly ObserverArtCatalog _art;
    public Vector2 CollisionAnchor => new(800, 270);
    public float CollisionFlash { get; private set; }
    public IReadOnlyList<Vector2> ContentPositions => _content.Select(c => c.Position).ToArray();

    public ObserverPendingPanel(Control canvas, ObserverPreviewSkin skin, ObserverArtCatalog art)
    {
        _art = art;
        for (int i = 0; i < 2; i++)
        {
            var origin = _origins[i];
            Panel(canvas, new Rect2(origin, new Vector2(150, 175)), skin.Header);
            var emptyBox = (StyleBoxFlat)skin.Cell.Duplicate();
            emptyBox.BgColor = i == 0 ? new Color("91b9a0") : new Color("d39c79");
            Panel(canvas, new Rect2(origin + new Vector2(10, 10), new Vector2(130, 116)), emptyBox);
            _empty[i] = Text(canvas, "尚无待发\n空槽", new Rect2(origin + new Vector2(10, 40), new Vector2(130, 110)), 18, PaperMuted, HorizontalAlignment.Center);
            var container = _content[i] = new Control { Position = origin, Size = new Vector2(150, 175), MouseFilter = Control.MouseFilterEnum.Ignore };
            canvas.AddChild(container);
            var artBox = (StyleBoxFlat)skin.Cell.Duplicate();
            artBox.BgColor = i == 0 ? new Color("91b9a0") : new Color("d39c79");
            Panel(container, new Rect2(10, 10, 130, 116), artBox);
            _images[i] = Image(container, null, new Rect2(22, 15, 106, 105), true);
            _names[i] = Text(container, "", new Rect2(7, 128, 136, 26), 20, PaperInk, HorizontalAlignment.Center);
            _details[i] = Text(container, "", new Rect2(5, 154, 140, 19), 13, PaperMuted, HorizontalAlignment.Center);
        }
        _title = Text(canvas, "待 发", new Rect2(746, 229, 108, 35), 24, Ink, HorizontalAlignment.Center);
        _value = Text(canvas, "— : —", new Rect2(744, 270, 112, 40), 26, new Color("ffe99a"), HorizontalAlignment.Center);
        _status = Text(canvas, "等待出招", new Rect2(744, 315, 112, 25), 14, Ink, HorizontalAlignment.Center);
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
            _names[i].Text = item?.Name ?? "";
            _details[i].Text = item == null ? "" : $"{ContestName(item.ContestType)} · {ObserverArtCatalog.ElementName(item.Element)}";
            if (contestCue?.Event.Contest is not { } active || item == null) continue;
            float p = (float)contestCue.Progress;
            float approach = p < 0.46f ? Ease(p / 0.46f) : p < 0.61f ? 1 : 1 - Ease((p - 0.61f) / 0.39f);
            float shake = p >= 0.46f && p < 0.75f ? MathF.Sin(p * 110) * 2.8f * (1 - p) : 0;
            _content[i].Position += new Vector2((i == 0 ? 1 : -1) * 68 * approach, shake);
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
