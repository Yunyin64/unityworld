namespace UnityWorld.Game.Domain;

public static partial class WorldMapGenerator
{
    private static void AuditHydrologyMetrics(Context c)
    {
        var m = c.World.Diagnostics.Metrics;
        var eligible = Enumerable.Range(0, c.World.Cells.Length).Where(i => EligibleLand(c, i)).ToArray();
        int ordinary = eligible.Count(i => c.World.Cells[i].RegionIndex is int r && c.Catalog.Regions[c.World.Regions[r].DefineId].GenerationPhase == "ordinary");
        m["eligibleLand"] = eligible.Length; m["ordinaryLand"] = ordinary;
        c.World.Diagnostics.RegionCoverage = eligible.Length == 0 ? 0 : ordinary / (double)eligible.Length;
        var low = c.World.Cells.Where(x => !x.IsFixed && x.Environment.Elevation < c.Profile.Hydrology.SeaLevel).ToArray();
        m["lowSeaCells"] = low.Length;
        m["seaLevelSatisfaction"] = low.Length == 0 ? 1 : low.Count(x => x.Water.Kind == "ocean") / (double)low.Length;
        if (m["seaLevelSatisfaction"] != 1) c.Fail("audit", "Reserved sea mask changed.");
        m["oceanArea"] = c.World.Cells.Count(x => x.Water.Kind == "ocean");
        m["lakeCount"] = c.World.Lakes.Count; m["lakeArea"] = c.World.Lakes.Sum(x => x.CellIndices.Count);
        m["lakeRegionArea"] = c.World.Regions.Where(r => c.Catalog.Regions[r.DefineId].GenerationPhase == "lake").Sum(r => r.CellIndices.Count);
        m["riverArea"] = c.World.Cells.Count(x => x.Water.Kind == "river");
        m["riverSources"] = c.World.Cells.Count(x => x.Water.Kind == "river" && x.Water.IsSource);
        m["riverConfluences"] = c.World.Cells.Count(x => x.Water.Kind == "river" && System.Numerics.BitOperations.PopCount((uint)x.Water.IncomingMask) > 1);
        m["maxRunoff"] = c.World.Cells.Max(x => x.Water.Runoff);
        for (int w = 1; w <= 3; w++) m["riverWidth" + w] = c.World.Cells.Count(x => x.Water.IsCenterline && x.Water.Width == w);
        int minGap = int.MaxValue;
        var gradients = new[] { new List<double>(), new List<double>(), new List<double>() };
        var seams = new[] { new List<double>(), new List<double>(), new List<double>() };
        int cliffEdges = 0;
        for (int i = 0; i < c.World.Cells.Length; i++)
        {
            var cell = c.World.Cells[i];
            if (cell.Water.Kind == "river" && !cell.Water.IsCenterline)
            {
                int owner = cell.Water.CenterlineIndex;
                if (owner < 0 || owner >= c.World.Cells.Length || !c.World.Cells[owner].Water.IsCenterline || cell.Id.DistanceTo(c.World.Cells[owner].Id) != 1 || cell.Water.Active)
                    c.Fail("water", $"Invalid river band owner/direction at {cell.Id}.");
            }
            if(!double.IsFinite(cell.Water.Runoff) || cell.Water.Runoff<0) c.Fail("water", "Invalid runoff.");
            if(cell.Water.IsCenterline && cell.Water.Width>1)
            {
                int d=cell.Water.OutDirection;
                bool Fits(int offset)=>new[]{(d+offset)%6,(d+offset+3)%6}.Take(cell.Water.Width-1).All(v=>c.Neighbors[i][v]>=0 && c.World.Cells[c.Neighbors[i][v]].Water.Kind is "river" or "lake" or "ocean");
                if(!Fits(2) && !Fits(1)) c.Fail("water", $"Required actual river width missing at {cell.Id}.");
            }
            foreach (int n in c.Neighbors[i].Where(n => n > i))
            {
                var other = c.World.Cells[n];
                bool Cliff(GeneratedCell x) => x.IsFixed && c.Catalog.Regions[c.World.Regions[x.RegionIndex.Value].DefineId].AllowEnvironmentCliff;
                bool cliff = Cliff(cell) != Cliff(other) && (Cliff(cell) || Cliff(other));
                if (cliff) { cliffEdges++; continue; }
                double[] diff = { Math.Abs(cell.Environment.Elevation - other.Environment.Elevation), Math.Abs(cell.Environment.Moisture - other.Environment.Moisture), Math.Abs(cell.Environment.Temperature - other.Environment.Temperature) };
                for (int a = 0; a < 3; a++) { gradients[a].Add(diff[a]); if (cell.RegionIndex != other.RegionIndex) seams[a].Add(diff[a]); }
            }
        }
        // Exact closest footprint pair via a multi-source hex Voronoi traversal, including holes/parity.
        var regionOwners=Enumerable.Repeat(-1,c.World.Cells.Length).ToArray();var distance=new int[regionOwners.Length];var queue=new Queue<int>();
        for(int i=0;i<regionOwners.Length;i++) if(!c.World.Cells[i].IsFixed && c.World.Cells[i].RegionIndex is int r) {regionOwners[i]=r;queue.Enqueue(i);}
        while(queue.TryDequeue(out int i)) foreach(int n in c.Neighbors[i].Where(n=>n>=0))
        {
            if(regionOwners[n]<0) {regionOwners[n]=regionOwners[i];distance[n]=distance[i]+1;queue.Enqueue(n);}
            else if(regionOwners[n]!=regionOwners[i]) minGap=Math.Min(minGap,distance[n]+distance[i]);
        }
        if(minGap<c.Profile.RegionGap) c.Fail("audit", $"Region footprint gap violation: {minGap}, requires {c.Profile.RegionGap}.");
        m["minimumObservedRegionGap"] = minGap == int.MaxValue ? -1 : minGap;
        m["explicitCliffEdges"] = cliffEdges;
        foreach(var group in c.World.Regions.Where(r=>r.IsFixed).GroupBy(r=>r.DefineId))
        {
            var cells=group.SelectMany(r=>r.CellIndices).Select(i=>c.World.Cells[i].Environment).ToArray();
            m[group.Key+"ElevationRange"]=cells.Max(e=>e.Elevation)-cells.Min(e=>e.Elevation);
            m[group.Key+"MoistureRange"]=cells.Max(e=>e.Moisture)-cells.Min(e=>e.Moisture);
            m[group.Key+"TemperatureRange"]=cells.Max(e=>e.Temperature)-cells.Min(e=>e.Temperature);
        }
        string[] axes = { "elevation", "moisture", "temperature" };
        for (int a = 0; a < 3; a++)
        {
            gradients[a].Sort(); seams[a].Sort();
            m[axes[a] + "GradientP95"] = gradients[a].Count == 0 ? 0 : gradients[a][(int)((gradients[a].Count - 1) * .95)];
            m[axes[a] + "GradientMax"] = gradients[a].Count == 0 ? 0 : gradients[a][^1];
            m[axes[a] + "SeamMax"] = seams[a].Count == 0 ? 0 : seams[a][^1];
        }
    }
}
