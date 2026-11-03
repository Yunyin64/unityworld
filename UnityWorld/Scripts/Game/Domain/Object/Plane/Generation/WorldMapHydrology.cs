using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain;

public static partial class WorldMapGenerator
{
    private static void ClassifySea(Context c)
    {
        foreach (var cell in c.World.Cells)
        {
            bool authoredSea = cell.IsFixed && c.Catalog.Tiles[cell.TileDefineId].WaterKind == "ocean";
            if (authoredSea || !cell.IsFixed && cell.Environment.Elevation < c.Profile.Hydrology.SeaLevel)
            {
                cell.Water.Kind = "ocean"; cell.Water.BodyId = "ocean";
                cell.Water.SurfaceLevel = c.Profile.Hydrology.SeaLevel;
                cell.Water.IsSink = true;
                cell.Water.Reason = authoredSea ? "fixed-ocean" : "natural-elevation-below-sea-level";
            }
            else if (cell.IsFixed && cell.Environment.Elevation < c.Profile.Hydrology.SeaLevel)
                c.World.Diagnostics.Reject("sea.fixed-land-exception", cell.Id.ToString());
        }
    }

    private static void PlaceLakes(Context c)
    {
        var definitions = c.Regions.Values.Where(x => x.GenerationPhase == "lake").ToArray();
        var slots = definitions.SelectMany(x => Enumerable.Repeat(x, x.MinCount)).ToArray();
        int attempts = 0, backtracks = 0;
        var random = c.Random("lake-placement");
        bool Search(int depth)
        {
            if (depth == slots.Length) return true;
            var def = slots[depth];
            var pool = AnchorPool(c, def).ToArray();
            for (int i = pool.Length - 1; i > 0; i--) { int j = random.Next(i + 1); (pool[i], pool[j]) = (pool[j], pool[i]); }
            // Fair per-slot search leaves budget for later mandatory lakes.
            int quota = Math.Max(1, c.Profile.LakePlacementAttempts / Math.Max(1, slots.Length * 2));
            foreach (int anchor in pool.Take(quota))
            {
                if (++attempts > c.Profile.LakePlacementAttempts) return false;
                if (!TryRegion(c, def, anchor, out var plan)) continue;
                bool portsValid = def.Lake.Ports.All(p =>
                {
                    int n = c.Index(c.World.Cells[anchor].Id + new TileId(p.Dq, p.Dr).Neighbor(p.Direction));
                    return n >= 0 && c.World.Cells[n].TileDefineId == null && !c.World.Cells[n].HasForcedLandmark && c.World.Cells[n].Water.Kind == "none";
                });
                if (!portsValid) { c.World.Diagnostics.Reject("lake.port-blocked", def.ID); continue; }
                var region = CommitRegion(c, plan);
                if (Search(depth + 1)) return true;
                UndoRegion(c, plan, region);
                if (++backtracks > c.Profile.RegionBacktrackBudget) return false;
            }
            return false;
        }
        if (!Search(0)) c.Fail("lakes", "Required lakes unsatisfied within budget: " + string.Join(", ", definitions.Select(x => $"{x.ID}: missing {x.MinCount - c.PlacedByType[x.ID].Count}")));
        c.World.Diagnostics.Metrics["lakePlacementAttempts"] = attempts;
        foreach (var region in c.World.Regions.Where(x => c.Catalog.Regions[x.DefineId].GenerationPhase == "lake"))
        {
            var def = c.Catalog.Regions[region.DefineId];
            var lake = new GeneratedLake { Id = $"lake-{region.Index}", RegionIndex = region.Index, Closed = def.Lake.Role == "closed",
                CellIndices = region.CellIndices.Where(i => c.Catalog.Tiles[c.World.Cells[i].TileDefineId].WaterKind == "lake").ToList() };
            lake.SurfaceLevel = lake.CellIndices.Max(i => c.World.Cells[i].Environment.Elevation);
            foreach (int i in lake.CellIndices)
            {
                var w = c.World.Cells[i].Water;
                w.Kind = "lake"; w.BodyId = lake.Id; w.SurfaceLevel = lake.SurfaceLevel; w.Reason = "authored-lake-region";
            }
            foreach (var port in def.Lake.Ports)
            {
                int i = c.Index(region.Anchor + new TileId(port.Dq, port.Dr));
                if (port.IsInlet) lake.InletCells.Add(i);
                else { lake.OutletCell = i; lake.OutletTarget = c.Neighbors[i][port.Direction]; }
            }
            c.World.Lakes.Add(lake);
        }
    }

    // Directed drainage graph: lake water is one node, shore/island cells are obstacles.
    // Reverse adjacency is flooded from legal terminals; parent ranks strictly decrease.
    private static void PlanHydrology(Context c)
    {
        var settings = c.Profile.Hydrology;
        if (settings.RiverCount == 0) return;
        int count = c.World.Cells.Length, size = count + c.World.Lakes.Count;
        var authored=new Dictionary<int,RegionRiverCell>();var narrow=new HashSet<int>();
        foreach(var region in c.World.Regions.Where(r=>r.IsFixed))
        {
            var def=c.Catalog.Regions[region.DefineId];
            foreach(var river in def.RiverCells) authored[c.Index(region.Anchor+new TileId(river.Dq,river.Dr))]=river;
            foreach(var point in def.NarrowRiverCells) narrow.Add(c.Index(region.Anchor+new TileId(point.Dq,point.Dr)));
        }
        var requiredNext=new Dictionary<int,int>();
        foreach(var (index,river) in authored) foreach(int direction in river.InDirections)
        {
            int from=c.Neighbors[index][direction];
            if(from<0) c.Fail("hydrology", "Authored inlet exits world.");
            if(requiredNext.TryGetValue(from,out int existing) && existing!=index) c.Fail("hydrology", "Conflicting authored inlet.");
            requiredNext[from]=index;
        }
        for(int k=0;k<c.World.Lakes.Count;k++)
        {
            var lake=c.World.Lakes[k];var region=c.World.Regions[lake.RegionIndex];var def=c.Catalog.Regions[region.DefineId];
            foreach(var port in def.Lake.Ports.Where(p=>p.IsInlet))
            {
                int outside=c.Index(region.Anchor+new TileId(port.Dq,port.Dr).Neighbor(port.Direction));
                if(requiredNext.ContainsKey(outside)) c.Fail("hydrology", "Lake inlet conflicts with authored river port.");
                requiredNext[outside]=count+k;
            }
        }
        var nodes = Enumerable.Range(0, count).ToArray();
        for (int k = 0; k < c.World.Lakes.Count; k++) foreach (int i in c.World.Lakes[k].CellIndices) nodes[i] = count + k;
        var reverse = Enumerable.Range(0, size).Select(_ => new List<int>()).ToArray();
        var parent = Enumerable.Repeat(-1, size).ToArray();
        var rank = Enumerable.Repeat(-1, size).ToArray();
        var height = new double[size]; var runoff = new double[size]; var visited = new bool[size];
        var edge = new Dictionary<(int from, int to), (int cell, int next)>();
        bool Land(int i) => i >= 0 && (c.World.Cells[i].Water.Kind == "none" && c.World.Cells[i].TileDefineId == null || authored.ContainsKey(i));
        bool Surface(int i) => i >= 0 && c.World.Cells[i].Water.Kind == "ocean";
        // Full width clearance is reserved before accumulation, so a later large trunk cannot
        // force an undocumented one-cell pinch beside protected mountains or lake shores.
        bool BandCell(int i) => i >= 0 && (c.World.Cells[i].TileDefineId == null || authored.ContainsKey(i) || c.World.Cells[i].Water.Kind is "lake" or "ocean");
        bool Center(int i) => Land(i) && (narrow.Contains(i) || Enumerable.Range(0, 6).Any(d => BandCell(c.Neighbors[i][(d + 2) % 6]) && BandCell(c.Neighbors[i][(d + 5) % 6])));
        void Edge(int from, int to, int cell, int next)
        {
            if (from == to) return;
            reverse[to].Add(from); edge[(from, to)] = (cell, next);
        }
        for (int i = 0; i < count; i++)
        {
            height[i] = Surface(i) ? settings.SeaLevel : c.World.Cells[i].Environment.Elevation;
            if (!Land(i)) continue;
            runoff[i] = Math.Max(0, c.World.Cells[i].Environment.Moisture);
            foreach (int n in c.Neighbors[i]) if (n >= 0 && (Center(n) || Surface(n)))
            {
                int d=Array.IndexOf(c.Neighbors[i],n);
                if(requiredNext.TryGetValue(i,out int requiredTarget) && n!=requiredTarget) continue;
                if(authored.TryGetValue(i,out var river) && river.OutDirection!=d) continue;
                if(authored.TryGetValue(n,out var target) && !target.InDirections.Contains((d+3)%6)) continue;
                if (narrow.Contains(i) || new[]{2,1}.Any(offset=>BandCell(c.Neighbors[i][(d+offset)%6]) && BandCell(c.Neighbors[i][(d+offset+3)%6]))) Edge(i,n,i,n);
            }
        }
        for (int k = 0; k < c.World.Lakes.Count; k++)
        {
            var lake = c.World.Lakes[k]; int node = count + k;
            height[node] = lake.SurfaceLevel;
            runoff[node] = lake.CellIndices.Sum(i => c.World.Cells[i].Environment.Moisture);
            var region = c.World.Regions[lake.RegionIndex]; var def = c.Catalog.Regions[region.DefineId];
            foreach(int shore in region.CellIndices.Except(lake.CellIndices))
            { runoff[shore]=Math.Max(0,c.World.Cells[shore].Environment.Moisture); Edge(shore,node,shore,lake.CellIndices[0]); }
            foreach (var port in def.Lake.Ports)
            {
                int i = c.Index(region.Anchor + new TileId(port.Dq, port.Dr)), n = c.Neighbors[i][port.Direction];
                if (!Land(n) && !Surface(n)) c.Fail("hydrology", $"Lake {lake.Id} port is blocked at {i}.");
                if (port.IsInlet) Edge(n, node, n, i); else Edge(node, n, i, n);
            }
        }
        var heap = new PriorityQueue<int, (double height, int node)>();
        void Root(int node) { if (visited[node]) return; visited[node] = true; heap.Enqueue(node, (height[node], node)); }
        for (int i = 0; i < count; i++) if (Surface(i) && (c.World.Cells[i].TileDefineId == null && settings.OceanTypeIds.Any(id=>c.Catalog.Tiles[id].AllowFill) || settings.OceanTypeIds.Contains(c.World.Cells[i].TileDefineId))) Root(i);
        if (!settings.RequireOceanExit && settings.AllowInlandLakes) foreach (var p in settings.LakeSinks)
        {
            int i = p.Row * c.World.Width + p.Col;
            if (!Land(i)) c.Fail("hydrology", $"Explicit lake sink overlaps protected content at {i}.");
            c.World.Cells[i].Water.Kind = "lake"; c.World.Cells[i].Water.BodyId = $"sink-{i}";
            c.World.Cells[i].Water.IsSink = true; c.World.Cells[i].Water.IsSource = true; Root(i);
        }
        for (int k = 0; k < c.World.Lakes.Count; k++) if (c.World.Lakes[k].Closed) Root(count + k);
        if (heap.Count == 0) c.Fail("hydrology", "No legal ocean or explicitly declared closed lake outlet.");
        int sequence = 0, budget = 0;
        var order = new List<int>();
        while (heap.TryDequeue(out int node, out var priority))
        {
            rank[node] = sequence++; order.Add(node);
            foreach (int upstream in reverse[node])
            {
                if (++budget > settings.RoutingBudget) c.Fail("hydrology", "Drainage routing budget exhausted.");
                if (visited[upstream]) continue;
                visited[upstream] = true; parent[upstream] = node;
                height[upstream] = Math.Max(height[upstream], priority.height);
                heap.Enqueue(upstream, (height[upstream], upstream));
            }
        }
        foreach(int i in authored.Keys) if(rank[i]<0) c.Fail("hydrology", $"Authored river has no compatible downstream route at {i}.");
        foreach (var lake in c.World.Lakes)
            if (rank[count + c.World.Lakes.IndexOf(lake)] < 0) c.Fail("hydrology", $"Lake {lake.Id} has no compatible route from its unique outlet.");
        for(int k=0;k<c.World.Lakes.Count;k++)
        {
            var lake=c.World.Lakes[k];lake.SurfaceLevel=height[count+k];
            foreach(int i in lake.CellIndices) c.World.Cells[i].Water.SurfaceLevel=lake.SurfaceLevel;
        }
        var local=runoff.ToArray();
        foreach (int node in order.AsEnumerable().Reverse()) if (parent[node] >= 0) runoff[parent[node]] += runoff[node];
        for (int i = 0; i < count; i++)
        {
            int n = nodes[i]; var w = c.World.Cells[i].Water;
            w.LocalRunoff=local[n]; w.DrainageTarget=parent[n];
            w.DrainageElevation = height[n]; w.DrainageRank = rank[n]; w.Runoff = runoff[n]; c.DrainOrder[i] = rank[n];
        }
        // Select all above-threshold drainage branches, including the sole downstream of every open lake.
        var selected = new HashSet<int>(order.Where(n => n >= count || n < count && Land(n) && (authored.ContainsKey(n) || requiredNext.ContainsKey(n) || runoff[n] >= settings.TributaryThreshold)));
        foreach (int n in selected.ToArray())
        {
            int at = n;
            while (parent[at] >= 0 && selected.Add(parent[at])) at = parent[at];
        }
        foreach (int node in selected.OrderBy(x => rank[x]))
        {
            if (node >= count) { c.World.Lakes[node-count].LocalRunoff=local[node]; c.World.Lakes[node - count].Runoff = runoff[node]; c.World.Lakes[node-count].DrainageTarget=parent[node]; continue; }
            var w = c.World.Cells[node].Water;
            if (w.Kind == "ocean" || w.IsSink) continue;
            w.Kind = "river"; w.BodyId = "river-network"; w.IsCenterline = true; w.CenterlineIndex = node;
            w.Width = runoff[node] >= settings.WideRiverThreshold ? settings.MaxRiverWidth : runoff[node] >= settings.MainRiverThreshold ? settings.MainRiverWidth : 1;
            w.Reason = "accumulated-moisture-runoff";
            if(narrow.Contains(node) && w.Width>1) { c.World.Diagnostics.Repairs.Add($"explicit narrow canyon at {c.World.Cells[node].Id}: {w.Width}->1");w.Width=1;w.Reason="authored-narrow-canyon"; }
        }
        foreach (int node in selected.OrderBy(x => rank[x]))
        {
            int next = parent[node]; if (next < 0) continue;
            if (!edge.TryGetValue((node, next), out var e)) c.Fail("hydrology", $"Missing drainage edge {node}->{next}.");
            var from = c.World.Cells[e.cell].Water; var to = c.World.Cells[e.next].Water;
            int d = Array.IndexOf(c.Neighbors[e.cell], e.next);
            from.OutDirection = d; to.IncomingMask |= 1 << ((d + 3) % 6);
        }
        foreach (var cell in c.World.Cells.Where(x => x.Water.IsCenterline)) cell.Water.IsSource = cell.Water.IncomingMask == 0;
        // Allocate deterministic perpendicular cross-sections; never shrink a required width.
        foreach (int i in selected.Where(x => x < count && c.World.Cells[x].Water.IsCenterline).OrderByDescending(x => runoff[x]).ThenBy(x => rank[x]))
        {
            var w = c.World.Cells[i].Water; if (w.Width == 1) continue;
            int d = w.OutDirection;
            var pairs = new[] { new[] { (d + 2) % 6, (d + 5) % 6 }, new[] { (d + 1) % 6, (d + 4) % 6 } };
            var options = pairs.Select(p => p.Select(v => c.Neighbors[i][v]).ToArray()).Where(p => p.All(BandCell)).ToArray();
            if (options.Length == 0) c.Fail("hydrology", $"Required width {w.Width} blocked at {c.World.Cells[i].Id}; no silent narrowing.");
            foreach (int side in options[0].Take(w.Width - 1))
            {
                var sideWater = c.World.Cells[side].Water;
                if (sideWater.Kind is "lake" or "ocean" || sideWater.IsCenterline) continue;
                if (sideWater.Kind == "river") continue; // Earlier larger flow owns overlaps.
                sideWater.Kind = "river"; sideWater.BodyId = w.BodyId; sideWater.CenterlineIndex = i;
                sideWater.Width = w.Width; sideWater.Reason = "river-band-side";
            }
        }
        c.World.Diagnostics.RiverCellCount = c.World.Cells.Count(x => x.Water.Kind == "river");
        AuditWater(c);
    }
}
