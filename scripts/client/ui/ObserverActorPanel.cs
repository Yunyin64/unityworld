using Godot;
using System.Linq;
using Xianxia.Client.Battle;
using static Xianxia.Client.UI.ObserverWidgets;

namespace Xianxia.Client.UI;

/// <summary>人物身份和属性面板，只显示投影出的当前值及真实上限。</summary>
internal sealed class ObserverActorPanel
{
    private readonly Label _name, _actorName, _buffs;
    private readonly Label[] _values = new Label[4];
    public Vector2 HeadAnchor { get; }

    public ObserverActorPanel(Control canvas, ObserverPreviewSkin skin, int side)
    {
        float sx = side == 0 ? 22 : 1126;
        Image(canvas, skin.Stats, new Rect2(sx, 750, 452, 133));
        Image(canvas, skin.Portraits[side], new Rect2(sx + (side == 0 ? 29 : 22), 778, 70, 76), true);
        _name = Text(canvas, "", new Rect2(sx + 115, 761, 290, 27), 19, PaperInk);
        Field(canvas, sx + 115, 791, 310, "气血", new Color("a83d31"), 0);
        Field(canvas, sx + 115, 818, 310, "灵力", new Color("286c84"), 1);
        Field(canvas, sx + 115, 845, 190, "SP占用", new Color("917124"), 2);
        Field(canvas, sx + 311, 845, 114, "护盾", new Color("586e65"), 3);
        Image(canvas, skin.Characters[side], side == 0 ? new Rect2(458, 446, 230, 221) : new Rect2(933, 474, 219, 184), true);
        float ax = side == 0 ? 460 : 924;
        _actorName = Text(canvas, "", new Rect2(ax, 668, 210, 28), 20, Ink, HorizontalAlignment.Center);
        _actorName.AddThemeColorOverride("font_outline_color", new Color("3a463b"));
        _actorName.AddThemeConstantOverride("outline_size", 3);
        _buffs = Text(canvas, "", new Rect2(ax - 40, 704, 290, 32), 15, Ink, HorizontalAlignment.Center);
        // 名称通道在头顶上方，伤害通道稍低；均为 1600×900 设计画布坐标。
        HeadAnchor = new Vector2(side == 0 ? 570 : 1038, side == 0 ? 472 : 493);
    }

    private void Field(Control canvas, float x, float y, float width, string name, Color color, int index)
    {
        canvas.AddChild(new ColorRect { Position = new Vector2(x, y), Size = new Vector2(width, 22), Color = new Color(color, 0.13f), MouseFilter = Control.MouseFilterEnum.Ignore });
        canvas.AddChild(new ColorRect { Position = new Vector2(x, y + 2), Size = new Vector2(3, 18), Color = color, MouseFilter = Control.MouseFilterEnum.Ignore });
        Text(canvas, name, new Rect2(x + 9, y, 70, 22), 15, PaperInk);
        _values[index] = Text(canvas, "—", new Rect2(x + 77, y, width - 83, 22), 17, color, HorizontalAlignment.Right);
    }

    public void Submit(NpcSnapshot npc)
    {
        _name.Text = npc.Name;
        _actorName.Text = npc.Name;
        _values[0].Text = Amount(npc.Hp, npc.HpMax);
        _values[1].Text = Amount(npc.Mp, npc.MpMax);
        _values[2].Text = Amount(npc.Sp, npc.SpMax);
        _values[3].Text = npc.Shield.ToString();
        foreach (var label in _values) label.TooltipText = npc.StatReadError ?? "当前值 / 当前上限；SP 为占用 / 角色上限，护盾无虚构上限。";
        string buffs = string.Join(" · ", npc.Buffs.Select(b => $"{b.Name}×{b.Stacks}"));
        _buffs.Text = buffs.Length == 0 ? (npc.Status == "Active" ? "" : npc.Status == "Defeated" ? "已败退" : npc.Status) : buffs;
        _buffs.TooltipText = buffs;
    }

    private static string Amount(int value, int? maximum) => maximum.HasValue ? $"{value}/{maximum.Value}" : $"{value}/?";
}
