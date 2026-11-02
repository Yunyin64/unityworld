using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain;

public static partial class WorldMapGenerator
{
    private static void GenerateEnvironment(Context c)
    {
        var settings = c.Profile.Environment;
        uint salt = c.Random("environment").NextUInt();
        for (int i = 0; i < c.World.Cells.Length; i++)
        {
            var cell = c.World.Cells[i];
            if (cell.TileDefineId != null) continue;
            // Axial -> Euclidean coordinates: all six neighbours have the same length.
            double x = cell.Id.Q + cell.Id.R * .5, y = cell.Id.R * Math.Sqrt(3) / 2;
            double east = cell.Col / (double)(c.World.Width - 1), south = cell.Row / (double)(c.World.Height - 1);
            double broad = Noise(x / settings.NoiseScale, y / settings.NoiseScale, salt);
            double detail = Noise(x / settings.DetailScale, y / settings.DetailScale, salt + 173);
            cell.Environment.Elevation = Math.Clamp(.53 - east * .24 + (broad - .5) * .72 + (detail - .5) * .16, .03, .97);
            cell.Environment.Moisture = Math.Clamp(settings.WestMoisture + (settings.EastMoisture - settings.WestMoisture) * east
                + (Noise(x / settings.NoiseScale, y / settings.NoiseScale, salt + 701) - .5) * .48, 0, 1);
            cell.Environment.Temperature = Math.Clamp(settings.NorthTemperature + (settings.SouthTemperature - settings.NorthTemperature) * south
                - .18 * (cell.Environment.Elevation - .4) + (Noise(x / settings.NoiseScale, y / settings.NoiseScale, salt + 992) - .5) * .18, 0, 1);
        }
        // Multi-source hex BFS, retaining each anchor's explicitly configured conditions.
        var distances = Enumerable.Repeat(int.MaxValue, c.World.Cells.Length).ToArray();
        var anchors = new int[c.World.Cells.Length];
        var queue = new Queue<int>();
        for (int i = 0; i < c.World.Cells.Length; i++) if (c.World.Cells[i].TileDefineId != null)
        { distances[i] = 0; anchors[i] = i; queue.Enqueue(i); }
        while (queue.TryDequeue(out int i))
        {
            if (distances[i] >= settings.BoundaryBlendDistance) continue;
            foreach (int n in c.Neighbors[i]) if (n >= 0 && distances[n] > distances[i] + 1)
            { distances[n] = distances[i] + 1; anchors[n] = anchors[i]; queue.Enqueue(n); }
        }
        for (int i = 0; i < distances.Length; i++)
        {
            if (distances[i] > 0 && distances[i] < settings.BoundaryBlendDistance)
            {
                var e = c.World.Cells[i].Environment; var a = c.World.Cells[anchors[i]].Environment;
                double f = 1 - distances[i] / (double)settings.BoundaryBlendDistance; f = f * f * (3 - 2 * f);
                e.Elevation += (a.Elevation - e.Elevation) * f;
                e.Moisture += (a.Moisture - e.Moisture) * f;
                e.Temperature += (a.Temperature - e.Temperature) * f;
            }
            if (!ValidEnvironment(c.World.Cells[i].Environment)) c.Fail("environment", $"Invalid environment at {c.World.Cells[i].Id}.");
        }
    }

    private static double Noise(double x, double y, uint salt)
    {
        int ix = (int)Math.Floor(x), iy = (int)Math.Floor(y);
        double fx = x - ix, fy = y - iy;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        double a = NoisePoint(ix, iy, salt), b = NoisePoint(ix + 1, iy, salt);
        double d = NoisePoint(ix, iy + 1, salt), e = NoisePoint(ix + 1, iy + 1, salt);
        return (a + (b - a) * fx) * (1 - fy) + (d + (e - d) * fx) * fy;
    }
    private static double NoisePoint(int x, int y, uint salt)
    {
        uint h = unchecked((uint)x * 374761393U + (uint)y * 668265263U + salt * 2246822519U);
        h = (h ^ (h >> 13)) * 1274126177U; h ^= h >> 16;
        return h / (double)uint.MaxValue;
    }

    private static void PlanHydrology(Context c)
    {
        if (c.Profile.Hydrology.RiverCount == 0) return;
        int count = c.World.Cells.Length;
        var visited = new bool[count]; var parent = Enumerable.Repeat(-1, count).ToArray();
        var heap = new PriorityQueue<int, (double height, int index)>();
        for (int i = 0; i < count; i++)
        {
            var cell = c.World.Cells[i];
            if (cell.TileDefineId != null && c.Profile.Hydrology.OceanTypeIds.Contains(cell.TileDefineId))
            { cell.Water.IsSink = true; visited[i] = true; heap.Enqueue(i, (cell.Environment.Elevation, i)); }
        }
        if (!c.Profile.Hydrology.RequireOceanExit && c.Profile.Hydrology.AllowInlandLakes)
            foreach (var p in c.Profile.Hydrology.LakeSinks)
            {
                int i = p.Row * c.World.Width + p.Col;
                if (c.World.Cells[i].TileDefineId != null) c.Fail("hydrology", $"Lake sink ({p.Col},{p.Row}) overlaps fixed boundary.");
                if (visited[i]) continue;
                visited[i] = true; c.World.Cells[i].Water.IsSink = true;
                heap.Enqueue(i, (c.World.Cells[i].Environment.Elevation, i));
            }
        if (heap.Count == 0) c.Fail("hydrology", "No legal ocean or explicitly configured lake outlet.");
        int order = 0;
        // Priority flood: surface depressions become drainage plateaus; parent order explicitly resolves equal heights.
        while (heap.TryDequeue(out int i, out var priority))
        {
            c.DrainOrder[i] = order++;
            for (int d = 0; d < 6; d++)
            {
                int n = c.Neighbors[i][d];
                if (n < 0 || visited[n] || c.World.Cells[n].TileDefineId != null) continue;
                visited[n] = true; parent[n] = i;
                c.World.Cells[n].Environment.Elevation = Math.Max(c.World.Cells[n].Environment.Elevation, priority.height);
                heap.Enqueue(n, (c.World.Cells[n].Environment.Elevation, n));
            }
        }
        var sources = Enumerable.Range(0, count).Where(i => c.World.Cells[i].TileDefineId == null && parent[i] >= 0
            && c.World.Cells[i].Col >= c.Profile.Hydrology.MinSourceColumn && c.World.Cells[i].Col <= c.Profile.Hydrology.MaxSourceColumn)
            .Select(i => (index: i, key: -Math.Log(Math.Max(1e-12, c.WaterRandom.NextDouble())) /
                Math.Max(.001, Math.Pow(c.World.Cells[i].Environment.Elevation, 2) * Math.Pow(c.World.Cells[i].Environment.Moisture, 2))))
            .OrderBy(x => x.key).ThenBy(x => x.index).ToArray();
        int installed = 0;
        var installedSources = new List<int>();
        foreach (var source in sources)
        {
            if (installed >= c.Profile.Hydrology.RiverCount) break;
            if (c.World.Cells[source.index].Water.Active) continue;
            // Avoid roots adjacent to another root; changing a source does not modify fixed boundary contents.
            if (installedSources.Any(i => c.World.Cells[i].Id.DistanceTo(c.World.Cells[source.index].Id) < 12)) continue;
            c.World.Cells[source.index].Water.IsSource = true;
            int at = source.index;
            while (!c.World.Cells[at].Water.IsSink)
            {
                int next = parent[at];
                if (next < 0) c.Fail("hydrology", $"No legal downstream from {c.World.Cells[at].Id}.");
                int direction = Array.IndexOf(c.Neighbors[at], next);
                c.World.Cells[at].Water.OutDirection = direction;
                c.World.Cells[next].Water.IncomingMask |= 1 << ((direction + 3) % 6);
                bool joins = c.World.Cells[next].Water.OutDirection >= 0;
                at = next;
                if (joins) break;
            }
            installed++; installedSources.Add(source.index);
        }
        if (installed != c.Profile.Hydrology.RiverCount) c.Fail("hydrology", $"Requested {c.Profile.Hydrology.RiverCount} sources but only {installed} legal sources exist.");
        c.World.Diagnostics.RiverCellCount = c.World.Cells.Count(x => x.Water.Active);
        AuditWater(c);
    }
}
