using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain;

public static partial class WorldMapGenerator
{
    // Fixed positions use absolute odd-r Offset anchors. Template cells remain relative Axial offsets.
    // Repeated anchors reuse the same small template; no border direction or theme lives in the algorithm.
    private static void InstallFixedRegions(Context c)
    {
        foreach (var def in c.Regions.Values.Where(d => d.ForcedPositions.Count > 0))
        foreach (var point in def.ForcedPositions.OrderBy(p => p.Row).ThenBy(p => p.Col))
        {
            var origin = Plane.OffsetToAxial(point.Col, point.Row);
            var plan = new RegionPlan { Define = def, Anchor = c.Index(origin), IsFixed = true, Score = 1 };
            foreach (var pair in def.TerrainLayout.OrderBy(p => p.Key.dr).ThenBy(p => p.Key.dq))
            {
                long q = (long)origin.Q + pair.Key.dq, row = (long)origin.R + pair.Key.dr;
                int index = q is < int.MinValue or > int.MaxValue || row is < int.MinValue or > int.MaxValue
                    ? -1 : c.Index(new TileId((int)q, (int)row));
                if (index < 0) c.Fail("fixed-regions", $"{def.ID} @ ({point.Col},{point.Row}) exceeds map at offset {pair.Key}.");
                if (c.World.Cells[index].TileDefineId != null) c.Fail("fixed-regions", $"{def.ID} overlaps another fixed region at {c.World.Cells[index].Id}.");
                plan.Cells.Add((index, pair.Value, def.FixedEnvironment.Copy()));
            }
            var localSingletons = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pair in def.LandMarkLayout.OrderBy(p => p.Key.dr).ThenBy(p => p.Key.dq))
            {
                int index = c.Index(origin + new TileId(pair.Key.dq, pair.Key.dr));
                var landmark = c.Catalog.LandMarks[pair.Value];
                if (landmark.IsSingleton && (c.Singletons.Contains(pair.Value) || !localSingletons.Add(pair.Value)))
                    c.Fail("fixed-regions", $"Fixed region {def.ID} repeats singleton {pair.Value}.");
                plan.Landmarks.Add((index, pair.Value));
            }
            CommitRegion(c, plan);
            foreach (var river in def.RiverCells)
            {
                var cell = c.World.Cells[c.Index(origin + new TileId(river.Dq, river.Dr))];
                cell.Water = new() { IncomingMask = river.InDirections.Aggregate(0, (mask, d) => mask | 1 << d),
                    OutDirection = river.OutDirection ?? -1, IsSource = river.IsSource, IsSink = river.IsSink };
            }
        }
        c.World.Diagnostics.BoundaryCount = c.World.Cells.Count(cell => cell.IsFixed);
    }

    // Position reservation only: actual natural/artificial entities are still created around the snapshot.
    private static void InstallFixedLandmarks(Context c)
    {
        foreach (var def in c.Catalog.LandMarks.Values.OrderBy(d => d.ID, StringComparer.Ordinal))
        foreach (var point in def.ForcedPositions.OrderBy(p => p.Row).ThenBy(p => p.Col))
        {
            var cell = c.World.At(point.Col, point.Row);
            if (cell.LandMarkDefineId != null) c.Fail("fixed-landmarks", $"Landmark {def.ID} conflicts at ({point.Col},{point.Row}).");
            if (def.IsSingleton && !c.Singletons.Add(def.ID)) c.Fail("fixed-landmarks", $"Singleton {def.ID} is already reserved.");
            cell.LandMarkDefineId = def.ID;
            cell.HasForcedLandmark = true;
        }
    }
}
