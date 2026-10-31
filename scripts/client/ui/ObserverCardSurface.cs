using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>把同一卡的相邻占格合成连续绘制面；外边距和未占格空洞仍保留。</summary>
internal sealed class ObserverCardSurface
{
    public IReadOnlyList<Rect2> Regions { get; }
    public IReadOnlyList<(Vector2 From, Vector2 To)> Outline { get; }
    private const float Epsilon = 0.01f;

    private ObserverCardSurface(IReadOnlyList<Rect2> regions)
    {
        Regions = regions;
        Outline = BuildOutline(regions);
    }

    public static ObserverCardSurface Create(IReadOnlyList<CellSnapshot> occupied, IReadOnlyList<Rect2> insetCells)
    {
        if (occupied.Count != insetCells.Count) return new ObserverCardSurface(MergeRegions(insetCells));
        var byCell = new Dictionary<(int X, int Y), Rect2>();
        for (int i = 0; i < occupied.Count; i++) byCell[(occupied[i].X, occupied[i].Y)] = insetCells[i];
        var joined = new List<Rect2>(occupied.Count);
        foreach (var cell in occupied)
        {
            var rect = byCell[(cell.X, cell.Y)];
            float left = rect.Position.X, right = rect.End.X, top = rect.Position.Y, bottom = rect.End.Y;
            // 只补真实四邻接之间的缝，各占一半；比例缩放后的入包卡也使用同一几何。
            if (byCell.TryGetValue((cell.X - 1, cell.Y), out var previous)) left = (left + previous.End.X) / 2;
            if (byCell.TryGetValue((cell.X + 1, cell.Y), out var next)) right = (right + next.Position.X) / 2;
            if (byCell.TryGetValue((cell.X, cell.Y - 1), out var above)) top = (top + above.End.Y) / 2;
            if (byCell.TryGetValue((cell.X, cell.Y + 1), out var below)) bottom = (bottom + below.Position.Y) / 2;
            joined.Add(new Rect2(left, top, right - left, bottom - top));
        }
        return new ObserverCardSurface(MergeRegions(joined));
    }

    // 水平扫描条带求并集，再合并相同横向范围的上下条带。
    // 矩形多格卡最终只剩一个矩形，因此图标不会在内部格线重新采样或被描边截断。
    private static IReadOnlyList<Rect2> MergeRegions(IReadOnlyList<Rect2> source)
    {
        var ys = source.SelectMany(r => new[] { r.Position.Y, r.End.Y }).Distinct().OrderBy(y => y).ToArray();
        var result = new List<Rect2>();
        for (int row = 0; row + 1 < ys.Length; row++)
        {
            float top = ys[row], bottom = ys[row + 1];
            if (bottom - top < Epsilon) continue;
            var spans = source.Where(r => r.Position.Y < bottom - Epsilon && r.End.Y > top + Epsilon)
                .Select(r => (Left: r.Position.X, Right: r.End.X)).OrderBy(r => r.Left).ToArray();
            int index = 0;
            while (index < spans.Length)
            {
                float left = spans[index].Left, right = spans[index++].Right;
                while (index < spans.Length && spans[index].Left <= right + Epsilon)
                    right = Math.Max(right, spans[index++].Right);
                int above = result.FindIndex(r => Near(r.Position.X, left) && Near(r.End.X, right) && Near(r.End.Y, top));
                if (above >= 0)
                {
                    var old = result[above];
                    result[above] = new Rect2(old.Position, new Vector2(old.Size.X, bottom - old.Position.Y));
                }
                else result.Add(new Rect2(left, top, right - left, bottom - top));
            }
        }
        return result;
    }

    private static IReadOnlyList<(Vector2 From, Vector2 To)> BuildOutline(IReadOnlyList<Rect2> regions)
    {
        var result = new List<(Vector2, Vector2)>();
        foreach (var rect in regions)
        {
            AddHorizontal(rect.Position.Y, rect.Position.X, rect.End.X,
                regions.Where(r => Near(r.End.Y, rect.Position.Y)), result);
            AddHorizontal(rect.End.Y, rect.Position.X, rect.End.X,
                regions.Where(r => Near(r.Position.Y, rect.End.Y)), result);
            AddVertical(rect.Position.X, rect.Position.Y, rect.End.Y,
                regions.Where(r => Near(r.End.X, rect.Position.X)), result);
            AddVertical(rect.End.X, rect.Position.Y, rect.End.Y,
                regions.Where(r => Near(r.Position.X, rect.End.X)), result);
        }
        return result;
    }

    private static void AddHorizontal(float y, float from, float to, IEnumerable<Rect2> covering, List<(Vector2, Vector2)> edges)
    {
        foreach (var part in Exposed(from, to, covering.Select(r => (r.Position.X, r.End.X))))
            edges.Add((new Vector2(part.From, y), new Vector2(part.To, y)));
    }

    private static void AddVertical(float x, float from, float to, IEnumerable<Rect2> covering, List<(Vector2, Vector2)> edges)
    {
        foreach (var part in Exposed(from, to, covering.Select(r => (r.Position.Y, r.End.Y))))
            edges.Add((new Vector2(x, part.From), new Vector2(x, part.To)));
    }

    // 只返回没有被另一块同卡绘制面遮住的边段，禁止内部边线穿过卡图。
    private static IEnumerable<(float From, float To)> Exposed(float from, float to, IEnumerable<(float From, float To)> covering)
    {
        float cursor = from;
        foreach (var span in covering.OrderBy(s => s.From))
        {
            float start = Math.Max(from, span.From), end = Math.Min(to, span.To);
            if (end <= start + Epsilon) continue;
            if (start > cursor + Epsilon) yield return (cursor, start);
            cursor = Math.Max(cursor, end);
            if (cursor >= to - Epsilon) yield break;
        }
        if (cursor < to - Epsilon) yield return (cursor, to);
    }

    private static bool Near(float a, float b) => Math.Abs(a - b) < Epsilon;
}
