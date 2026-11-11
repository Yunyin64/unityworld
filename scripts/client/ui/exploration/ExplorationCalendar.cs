using Godot;
using UnityWorld.Game.World;

namespace Xianxia.Client.UI.Exploration;

/// <summary>墨绿历牌及单枚月相。节日的 MoonPhase 为空，牌上仅显示核心日期。</summary>
internal partial class ExplorationCalendar : Control
{
    public MoonPhase? Moon { get; set; }
    public float MoonX { get; set; }

    public override void _Draw()
    {
        float w = Size.X, h = Size.Y;
        var outer = Outline(w, h, 1);
        DrawColoredPolygon(outer, new Color("283830"));
        DrawPolyline(outer, new Color("614e37"), 3, true);
        DrawPolyline(Outline(w, h, 3), new Color("cfb784"), 1.4f, true);
        DrawPolyline(Outline(w, h, 6), new Color("776e54"), 1, true);
        foreach (float x in new[] { 16f, w - 16f })
        {
            Vector2 p = new(x, h / 2);
            DrawColoredPolygon(new[] { p + Vector2.Up * 5, p + Vector2.Right * 3, p + Vector2.Down * 5, p + Vector2.Left * 3 }, new Color("b39a6c"));
        }
        if (Moon is not MoonPhase phase) return;
        var center = new Vector2(MoonX, h / 2);
        const float radius = 11;
        var ivory = new Color("e4d0a0");
        DrawCircle(center, radius, new Color("1c2523"));
        if (phase == MoonPhase.FullMoon) DrawCircle(center, radius, ivory);
        if (phase is MoonPhase.FirstQuarter or MoonPhase.LastQuarter)
        {
            var lit = new Vector2[25];
            float start = phase == MoonPhase.FirstQuarter ? -Mathf.Pi / 2 : Mathf.Pi / 2;
            for (int i = 0; i < lit.Length; ++i)
                lit[i] = center + Vector2.FromAngle(start + Mathf.Pi * i / (lit.Length - 1)) * radius;
            DrawColoredPolygon(lit, ivory);
        }
        DrawArc(center, radius, 0, Mathf.Tau, 40, new Color("beaa7b"), 1.4f, true);
    }

    private static Vector2[] Outline(float w, float h, float inset)
    {
        const float cut = 9;
        return new Vector2[] { new(inset + cut, inset), new(w - cut - inset, inset), new(w - inset, inset + cut),
            new(w - inset, h - cut - inset), new(w - cut - inset, h - inset), new(cut + inset, h - inset),
            new(inset, h - cut - inset), new(inset, inset + cut), new(inset + cut, inset) };
    }
}
