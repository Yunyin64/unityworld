using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>卡面按实际占格裁切，需求份额决定暗亮区域，不推导战斗规则。</summary>
internal static class ObserverCardPainter
{
    private static readonly string[] ElementOrder = { "Jin", "Mu", "Shui", "Huo", "Tu" };
    public static void Draw(Control view, ObserverArtCatalog art, CardSnapshot card, Rect2 bounds,
        IReadOnlyList<Rect2> cells, float alpha = 1, float flash = 0)
    {
        if (cells.Count == 0) return;
        var surface = ObserverCardSurface.Create(card.Cells, cells);
        cells = surface.Regions;
        var requirements = card.RequiredElements.OrderBy(e => Array.IndexOf(ElementOrder, e.Element))
            .SelectMany(e => Enumerable.Repeat(e.Element, Math.Max(0, e.Amount))).ToArray();
        var assigned = card.AssignedElements.GroupBy(e => e).ToDictionary(g => g.Key, g => g.Count());
        if (requirements.Length == 0)
            foreach (var cell in cells) view.DrawRect(cell, new Color(card.IsWound ? new Color("aa7068") : new Color("b6b08b"), alpha));
        for (int part = 0; part < requirements.Length; part++)
        {
            string element = requirements[part];
            bool lit = assigned.TryGetValue(element, out int count) && count > 0;
            if (lit) assigned[element] = count - 1;
            Color color = Bright(element);
            if (!lit) color = color.Lerp(new Color("586875"), 0.72f).Darkened(0.12f);
            float left = AreaCut(cells, bounds, (float)part / requirements.Length);
            float right = AreaCut(cells, bounds, (float)(part + 1) / requirements.Length);
            foreach (var cell in cells)
            {
                var segment = cell.Intersection(new Rect2(left, bounds.Position.Y, right - left, bounds.Size.Y));
                if (segment.HasArea()) view.DrawRect(segment, new Color(color, alpha));
            }
        }
        var artArea = new Rect2(bounds.Position + new Vector2(4, 18), new Vector2(Math.Max(1, bounds.Size.X - 8), Math.Max(1, bounds.Size.Y - 34)));
        var texture = art.Card(card.DefineId);
        if (texture != null) DrawClippedArt(view, texture, artArea, cells, art.CardTranspose(card.DefineId), alpha);
        // 缺图时仍保留实际形状、元素底色及名称，不额外跨空洞画占位图。
        foreach (var cell in cells)
        {
            if (flash > 0) view.DrawRect(cell, new Color(1, 0.97f, 0.63f, flash * alpha * 0.7f));
            if (card.IsFrozen)
            {
                view.DrawLine(cell.Position + new Vector2(4, 4), cell.End - new Vector2(4, 4), new Color("a7e6ff", alpha * 0.55f), 1);
            }
        }
        foreach (var edge in surface.Outline)
        {
            view.DrawLine(edge.From, edge.To, new Color(card.IsMain ? new Color("f8df9a") : new Color("637c83"), alpha), card.IsMain ? 2 : 1);
            if (card.IsFrozen) view.DrawLine(edge.From, edge.To, new Color("91dbff", alpha), 2);
        }
        var font = view.GetThemeDefaultFont();
        var nameArea = LabelArea(cells, false);
        var nameAt = nameArea.Position + new Vector2(3, 14);
        view.DrawStringOutline(font, nameAt, card.Name, HorizontalAlignment.Center, nameArea.Size.X - 6, 13, 3, new Color("263c43", alpha));
        view.DrawString(font, nameAt, card.Name, HorizontalAlignment.Center, nameArea.Size.X - 6, 13, new Color("f5f0d7", alpha));
        string tag = card.IsFrozen ? "冻结" : card.IsMain ? "主功法" : card.IsWound ? "伤口" : "";
        if (tag.Length == 0) return;
        var tagArea = LabelArea(cells, true);
        var at = tagArea.Position + new Vector2(2, tagArea.Size.Y - 3);
        view.DrawStringOutline(font, at, tag, HorizontalAlignment.Center, tagArea.Size.X - 4, 12, 2, new Color("263c43", alpha));
        view.DrawString(font, at, tag, HorizontalAlignment.Center, tagArea.Size.X - 4, 12, new Color("f5f0d7", alpha));
    }

    // 标签也选择实际连续占格；L 形底部的冻结标识不能横跨空洞。
    private static Rect2 LabelArea(IReadOnlyList<Rect2> cells, bool bottom)
    {
        float y = bottom ? cells.Max(c => c.Position.Y) : cells.Min(c => c.Position.Y);
        var row = cells.Where(c => Math.Abs(c.Position.Y - y) < 0.1f).OrderBy(c => c.Position.X).ToArray();
        var best = row[0];
        var current = best;
        foreach (var cell in row.Skip(1))
        {
            if (cell.Position.X - current.End.X < cell.Size.X * 0.2f) current = current.Merge(cell);
            else current = cell;
            if (current.Size.X > best.Size.X) best = current;
        }
        return best;
    }

    // 横向面积分位使 L 形等非矩形卡的需求份额相等，空洞不绘制。
    private static float AreaCut(IReadOnlyList<Rect2> cells, Rect2 bounds, float fraction)
    {
        if (fraction <= 0) return bounds.Position.X;
        if (fraction >= 1) return bounds.End.X;
        float target = cells.Sum(c => c.Size.X * c.Size.Y) * fraction;
        float lo = bounds.Position.X, hi = bounds.End.X;
        for (int i = 0; i < 18; i++)
        {
            float mid = (lo + hi) * 0.5f;
            float area = cells.Sum(c => Mathf.Clamp(mid - c.Position.X, 0, c.Size.X) * c.Size.Y);
            if (area < target) lo = mid; else hi = mid;
        }
        return (lo + hi) * 0.5f;
    }

    private static Color Bright(string element) => element switch
    {
        "Shui" => new Color("37b9f5"), "Jin" => new Color("f3cf65"), "Mu" => new Color("65db86"),
        "Huo" => new Color("fb986a"), "Tu" => new Color("d8af65"), _ => new Color("9dcddd")
    };

    private static void DrawClippedArt(Control view, Texture2D texture, Rect2 area, IReadOnlyList<Rect2> cells, bool transpose, float alpha)
    {
        Vector2 source = transpose ? new Vector2(texture.GetHeight(), texture.GetWidth()) : texture.GetSize();
        float scale = Math.Min(area.Size.X / source.X, area.Size.Y / source.Y);
        var target = new Rect2(area.GetCenter() - source * scale / 2, source * scale);
        if (transpose)
        {
            // AtlasTexture 的 transpose 会使区域方向错位；在旋转后的局部坐标裁切原图。
            var originalSize = texture.GetSize() * scale;
            view.DrawSetTransform(target.GetCenter(), -Mathf.Pi / 2);
            foreach (var cell in cells)
            {
                var clipped = target.Intersection(cell);
                if (!clipped.HasArea()) continue;
                var local = new Rect2(target.GetCenter().Y - clipped.End.Y, clipped.Position.X - target.GetCenter().X,
                    clipped.Size.Y, clipped.Size.X);
                var region = new Rect2((local.Position + originalSize / 2) / scale, local.Size / scale);
                view.DrawTextureRectRegion(texture, local, region, new Color(1, 1, 1, alpha * 0.84f));
            }
            view.DrawSetTransform(Vector2.Zero);
            return;
        }
        foreach (var cell in cells)
        {
            var clipped = target.Intersection(cell);
            if (!clipped.HasArea()) continue;
            var sourceRect = new Rect2((clipped.Position - target.Position) / scale, clipped.Size / scale);
            view.DrawTextureRectRegion(texture, clipped, sourceRect, new Color(1, 1, 1, alpha * 0.84f));
        }
    }
}
