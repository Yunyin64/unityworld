using System.Globalization;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain;

public static partial class WorldMapGenerator
{
    // 选址语法只在地图生成中解释，配置对象可以供其他内容类型复用。
    internal static (string Id, bool Exclude, int Radius, bool OrderOnly) ParseLandmarkCondition(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidDataException("LandMarkConditions contains an empty condition.");
        bool exclude = text.StartsWith("No:", StringComparison.Ordinal);
        string id = exclude ? text[3..] : text;
        bool orderOnly = id.StartsWith("After:", StringComparison.Ordinal);
        int radius = 0;
        if (orderOnly && !exclude) id = id[6..];
        else if (id.StartsWith("Near(", StringComparison.Ordinal))
        {
            int end = id.IndexOf("):", StringComparison.Ordinal);
            if (end < 0 || !int.TryParse(id.AsSpan(5, end - 5), NumberStyles.None, CultureInfo.InvariantCulture, out radius) || radius < 1)
                throw new InvalidDataException($"Invalid landmark condition '{text}'.");
            id = id[(end + 2)..];
        }
        if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny([':', '(', ')']) >= 0 || (orderOnly && exclude))
            throw new InvalidDataException($"Invalid landmark condition '{text}'.");
        return (id, exclude, radius, orderOnly);
    }

    // 注入固定的遍历顺序供校验使用，生成时则使用种子打乱的遍历顺序解决无依赖竞争。
    internal static IReadOnlyList<LandMarkDefine> OrderInitialLandmarks(IEnumerable<LandMarkDefine> definitions)
    {
        var active = definitions.Where(define => define.InitialPlacement?.IsEnabled == true)
            .ToDictionary(define => define.ID, StringComparer.Ordinal);
        var ordered = new List<LandMarkDefine>();
        var visiting = new List<string>();
        var pending = active.Keys.ToHashSet(StringComparer.Ordinal);
        foreach (var id in active.Keys) Visit(id);
        return ordered;

        void Visit(string id)
        {
            if (visiting.Contains(id))
                throw new InvalidDataException($"Landmark placement dependency cycle: {string.Join(" -> ", visiting.SkipWhile(value => value != id).Append(id))}.");
            if (!pending.Remove(id)) return;
            visiting.Add(id);
            foreach (var condition in active[id].InitialPlacement!.LandMarkConditions)
                Visit(ParseLandmarkCondition(condition).Id);
            visiting.RemoveAt(visiting.Count - 1);
            ordered.Add(active[id]);
        }
    }

    /// <summary>基于锁定后的地图和已有作者地标，规划独立地标。</summary>
    private static void ScatterLandmarks(Context c)
    {
        var positions = c.Catalog.LandMarks.Keys.ToDictionary(id => id, _ => new HashSet<TileId>(), StringComparer.Ordinal);
        foreach (var cell in c.World.Cells)
        foreach (var id in cell.PlannedLandMarks())
            positions[id].Add(cell.Id);

        var definitions = c.Catalog.LandMarks.Values.Where(define => define.InitialPlacement?.IsEnabled == true)
            .OrderBy(define => define.ID, StringComparer.Ordinal).ToArray();
        Shuffle(definitions, c.LandmarkRandom);
        // 先给仍有保底缺口的定义安排空间。明确声明的地标依赖仍需先完成规划。
        var prioritized = definitions.OrderByDescending(define =>
            (define.InitialPlacement!.NeedCount ?? 0) > positions[define.ID].Count);
        foreach (var define in OrderInitialLandmarks(prioritized))
        {
            var placement = define.InitialPlacement!;
            var existing = positions[define.ID];
            int authored = existing.Count;
            int cap = placement.MaxCount ?? int.MaxValue;
            if (authored > cap) c.Fail("landmarks", $"{define.ID}: {authored} author placements exceed MaxCount {cap}.");
            var random = c.Random("landmarks/" + define.ID);
            var siteConditions = placement.LandMarkConditions.Select(ParseLandmarkCondition).Where(rule => !rule.OrderOnly).ToArray();
            var acceptedTypes = c.Catalog.Tiles.Values.Where(tile => MatchesTerrainAndTags(placement, tile))
                .Select(tile => tile.ID).ToHashSet(StringComparer.Ordinal);
            var candidates = c.World.Cells.Where(cell => cell.LandMarkDefineId == null && !cell.HasForcedLandmark &&
                AllowsInitialLandmarks(c, cell)).ToArray();
            Shuffle(candidates, random);
            var eligible = candidates.Where(cell => acceptedTypes.Contains(cell.TileDefineId) &&
                siteConditions.All(rule => Within(cell.Id, positions[rule.Id], rule.Radius) != rule.Exclude)).ToArray();

            // 先按当前间距构造一组可落地的候选。裁去末尾以后，保留下来的任意前缀都仍然合法。
            int available = define.IsSingleton ? (c.Singletons.Contains(define.ID) ? 0 : 1) : candidates.Length;
            var rolled = Array.Empty<GeneratedCell>();
            if (placement.EnablesOrdinaryPlacement)
                rolled = placement.PerTileChance is > 0 and < 1
                    ? eligible.Where(cell => random.NextDouble() < placement.PerTileChance.Value).ToArray()
                    : eligible;
            var ordinary = SelectSites(rolled, existing, placement.MinSpacing, available);
            int possible = authored + ordinary.Count;
            int normal = possible;
            if (possible > cap)
            {
                int lower = Math.Max(authored, placement.MinCount ?? 0);
                normal = lower + random.Next(cap - lower + 1);
            }
            foreach (var cell in ordinary.Take(normal - authored)) Add(cell);
            if (placement.MinCount is int preferred && normal < preferred)
                c.World.Diagnostics.Warnings.Add($"{define.ID}: normal placements {normal} below MinCount {preferred}.");

            int need = placement.NeedCount ?? 0;
            int conditionFill = FillNeed(eligible);
            int fallback = FillNeed(candidates);
            var metrics = c.World.Diagnostics.Metrics;
            string key = "landmarks." + define.ID;
            metrics[key + ".authored"] = authored;
            metrics[key + ".eligible"] = eligible.Length;
            metrics[key + ".rolled"] = rolled.Length;
            metrics[key + ".possible"] = possible;
            metrics[key + ".normal"] = normal;
            metrics[key + ".conditionFill"] = conditionFill;
            metrics[key + ".fallback"] = fallback;
            metrics[key + ".total"] = existing.Count;
            if (existing.Count < need)
            {
                int free = candidates.Count(cell => cell.LandMarkDefineId == null);
                string limit = define.IsSingleton && c.Singletons.Contains(define.ID)
                    ? "IsSingleton already reserved"
                    : free == 0 ? "no writable sites remain under occupancy and region settings"
                    : $"{free} writable sites remain, all blocked by MinSpacing {placement.MinSpacing}";
                c.Fail("landmarks", $"{define.ID}: NeedCount {need}, placed {existing.Count}, missing {need - existing.Count}; {limit} in this attempt.");
            }

            void Add(GeneratedCell cell)
            {
                cell.AddPlannedLandMark(define.ID);
                cell.HasGeneratedLandMark = true;
                existing.Add(cell.Id);
                if (define.IsSingleton) c.Singletons.Add(define.ID);
            }

            int FillNeed(IEnumerable<GeneratedCell> pool)
            {
                int missing = Math.Min(available - (existing.Count - authored), need - existing.Count);
                var selected = SelectSites(pool.Where(cell => cell.LandMarkDefineId == null), existing, placement.MinSpacing, missing);
                foreach (var cell in selected) Add(cell);
                return selected.Count;
            }
        }
    }

    private static bool AllowsInitialLandmarks(Context c, GeneratedCell cell) =>
        cell.RegionIndex is int region ? c.Catalog.Regions[c.World.Regions[region].DefineId].AllowInitialLandMarks
            : cell.Source == TileGenerationSource.Fill && !cell.IsFixed;

    private static bool MatchesTerrainAndTags(InitialPlacement placement, TileDefine tile)
    {
        var allowed = placement.TerrainConditions.Where(value => !value.StartsWith("No:", StringComparison.Ordinal));
        return (!allowed.Any() || allowed.Contains(tile.ID)) && !placement.TerrainConditions.Contains("No:" + tile.ID) &&
            placement.TagConditions.All(value => value.StartsWith("No:", StringComparison.Ordinal)
                ? !tile.Tags.Contains(value[3..]) : tile.Tags.Contains(value));
    }

    private static List<GeneratedCell> SelectSites(IEnumerable<GeneratedCell> pool, HashSet<TileId> existing, int spacing, int limit)
    {
        var selected = new List<GeneratedCell>();
        if (limit <= 0) return selected;
        var occupied = spacing > 1 ? new HashSet<TileId>(existing) : null;
        foreach (var cell in pool)
        {
            if (occupied != null && Within(cell.Id, occupied, spacing - 1)) continue;
            selected.Add(cell);
            occupied?.Add(cell.Id);
            if (selected.Count == limit) break;
        }
        return selected;
    }

    private static bool Within(TileId origin, HashSet<TileId> positions, int radius)
    {
        if (radius == 0) return positions.Contains(origin);
        // 稀疏地标直接查已有位置，密集时只扫六边形范围。
        if (radius > 200 || 1 + 3 * radius * (radius + 1) >= positions.Count)
            return positions.Any(position => origin.DistanceTo(position) <= radius);
        for (int q = -radius; q <= radius; q++)
        for (int r = Math.Max(-radius, -q - radius); r <= Math.Min(radius, -q + radius); r++)
            if (positions.Contains(origin + new TileId(q, r))) return true;
        return false;
    }

    private static void Shuffle<T>(T[] items, MapRandom random)
    {
        for (int last = items.Length - 1; last > 0; last--)
        {
            int pick = random.Next(last + 1);
            (items[last], items[pick]) = (items[pick], items[last]);
        }
    }
}
