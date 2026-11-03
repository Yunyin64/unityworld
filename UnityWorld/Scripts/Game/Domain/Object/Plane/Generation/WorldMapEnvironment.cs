using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain;

public static partial class WorldMapGenerator
{
    private static void GenerateEnvironment(Context c)
    {
        var settings = c.Profile.Environment;
        uint salt = c.Random("environment").NextUInt(), fixedSalt = unchecked((uint)settings.FixedSeed);
        var influences = new List<(RegionDefine def, int[] distance)>();
        foreach (var def in c.Regions.Values.Where(x => x.ForcedPositions.Count > 0))
        {
            var distance = Enumerable.Repeat(int.MaxValue, c.World.Cells.Length).ToArray();
            var queue = new Queue<int>();
            foreach(var r in c.PlacedByType[def.ID]) foreach(int i in r.CellIndices) { distance[i]=0; queue.Enqueue(i); }
            while(queue.TryDequeue(out int i))
            {
                if(distance[i]>=(def.FixedBlendDistance??settings.BoundaryBlendDistance)) continue;
                foreach(int n in c.Neighbors[i]) if(n>=0 && distance[n]>distance[i]+1) { distance[n]=distance[i]+1; queue.Enqueue(n); }
            }
            influences.Add((def,distance));
        }
        for(int i=0;i<c.World.Cells.Length;i++)
        {
            var cell=c.World.Cells[i];
            var own=cell.IsFixed ? c.Catalog.Regions[c.World.Regions[cell.RegionIndex.Value].DefineId] : null;
            uint seed=cell.IsFixed ? fixedSalt : salt;
            double x=cell.Id.Q+cell.Id.R*.5, y=cell.Id.R*Math.Sqrt(3)/2;
            double east=cell.Col/(double)(c.World.Width-1), south=cell.Row/(double)(c.World.Height-1);
            double F(uint channel) => .68*Noise(x/settings.NoiseScale,y/settings.NoiseScale,seed+channel)
                +.25*Noise(x/settings.DetailScale,y/settings.DetailScale,seed+channel+173)
                +.07*Noise(x/(settings.DetailScale*.5),y/(settings.DetailScale*.5),seed+channel+311);
            var e=new MapEnvironment { Elevation=Math.Clamp(.55-east*.35+(F(0)-.5)*settings.ElevationAmplitude,0,1),
                Moisture=Math.Clamp(settings.WestMoisture+(settings.EastMoisture-settings.WestMoisture)*east+(F(701)-.5)*settings.MoistureAmplitude,0,1),
                Temperature=Math.Clamp(settings.NorthTemperature+(settings.SouthTemperature-settings.NorthTemperature)*south+(F(992)-.5)*settings.TemperatureAmplitude,0,1) };
            double total=0, elevation=0, moisture=0, temperature=0, strongest=0;
            foreach(var (def,distance) in influences)
            {
                if(own!=null && (!own.BlendFixedEnvironment || own.AllowEnvironmentCliff) && def.ID!=own.ID) continue;
                int blend=def.FixedBlendDistance??settings.BoundaryBlendDistance;
                double t=distance[i]==0 ? 1 : blend==0 ? 0 : Math.Max(0,1-distance[i]/(double)blend);
                double weight=t*t*t*(t*(t*6-15)+10);
                if(weight<=0) continue;
                var b=def.FixedEnvironment;var v=def.FixedVariation;
                double V(uint channel)=>2*Noise(x/settings.FixedVariationScale,y/settings.FixedVariationScale,fixedSalt+channel)-1;
                elevation+=weight*(b.Elevation+v.Elevation*V(17));
                moisture+=weight*(b.Moisture+v.Moisture*V(53));
                temperature+=weight*(b.Temperature+v.Temperature*V(97));
                total+=weight;strongest=Math.Max(strongest,weight);
            }
            if(total>0)
            {
                double f=cell.IsFixed?1:strongest;
                e.Elevation=f==1?elevation/total:e.Elevation+(elevation/total-e.Elevation)*f;
                e.Moisture=f==1?moisture/total:e.Moisture+(moisture/total-e.Moisture)*f;
                e.Temperature=f==1?temperature/total:e.Temperature+(temperature/total-e.Temperature)*f;
            }
            if(own!=null && c.Catalog.Tiles[cell.TileDefineId].WaterKind=="ocean") e.Elevation=Math.Min(e.Elevation,Math.Max(0,c.Profile.Hydrology.SeaLevel-1e-6));
            e.Elevation=Math.Clamp(e.Elevation,0,1);e.Moisture=Math.Clamp(e.Moisture,0,1);
            if(own==null || own.BlendFixedEnvironment) e.Temperature-=.18*(e.Elevation-.4);
            e.Temperature=Math.Clamp(e.Temperature,0,1);
            if(own!=null)
            {
                var limits=own.FixedEnvironmentRange;
                e.Elevation=Math.Clamp(e.Elevation,limits.Elevation.Min,limits.Elevation.Max);
                e.Moisture=Math.Clamp(e.Moisture,limits.Moisture.Min,limits.Moisture.Max);
                e.Temperature=Math.Clamp(e.Temperature,limits.Temperature.Min,limits.Temperature.Max);
                if(c.Catalog.Tiles[cell.TileDefineId].WaterKind=="ocean" && e.Elevation>=c.Profile.Hydrology.SeaLevel && c.Profile.Hydrology.SeaLevel>0) c.Fail("environment", $"Fixed ocean range conflicts with sea level at {cell.Id}.");
            }
            cell.Environment=e;cell.Water.DrainageElevation=e.Elevation;
            if(!ValidEnvironment(e)) c.Fail("environment", $"Invalid environment at {cell.Id}.");
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

}
