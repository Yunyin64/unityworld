using System.Collections.Generic;
using System.Linq;
using Godot;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.Exploration;

/// <summary>三维地形输入。视野外的一圈仅参与可见网格接缝计算。</summary>
internal sealed record ExplorationTerrainSource(int Seed, int PlaneId, IReadOnlySet<TileId> Visible,
    IReadOnlyDictionary<TileId, TerrainCell3D> Cells, IReadOnlyList<Vector2>? Trail = null, TileId? Gate = null)
{
    internal static ExplorationTerrainSource FromSnapshot(ExplorationSnapshot snapshot)
    {
        var visible = new HashSet<TileId>(snapshot.Tiles.Keys);
        var neighborhood = new HashSet<TileId>(visible);
        foreach (var id in visible)
            foreach (var offset in TileId.Directions)
                neighborhood.Add(id + offset);

        var catalog = GameDataMgr.Instance.MapCatalog;
        var plane = PlaneMgr.Instance.GetPlaneById(new PlaneId(snapshot.PlaneId));
        var cells = new Dictionary<TileId, TerrainCell3D>(neighborhood.Count);
        foreach (var id in neighborhood)
        {
            snapshot.Tiles.TryGetValue(id, out var entry);
            var tile = entry?.Tile ?? plane?.GetTile(id);
            if (tile == null) continue;
            catalog.Tiles.TryGetValue(tile.TypeId, out var define);
            cells.Add(id, TerrainCell3D.From(tile.Environment, tile.Water, define, entry?.LandmarkName ?? ""));
        }
        return new(snapshot.Seed, snapshot.PlaneId, visible, cells);
    }

    // 有意把相同的平原放在海岸、内陆和山脚，便于在场景里直接比较。
    private static readonly string[] SampleRows =
    {
        "~~~~~~~~~~~ffhmm~~~~~",
        "~~~~~~~~~~ffhmmm~~~~~",
        "~~~~~~g~gffhmssmh~~~~",
        "~~~~~gfggghmssmmm~~~~",
        "~~~~fgppffhmssmhh~~~~",
        "~~~~gfpPpfgmshhf~~~~~",
        "~~~ghffpppgGhhhhff~~~",
        "~~~~hfgbffghfffhg~~~~",
        "~~~hmmfippprpggf~~~~~",
        "~~~hmsmppprppg~~~~~~~",
        "~~~~hmmppprpg~~~~~~~~",
        "~~~~~~hfprg~~~~~~~~~~",
        "~~~~~~~fgr~~~~~~~~~~~",
        "~~~~~~~~g~~~~~~gf~~~~",
        "~~~~~~~~~~~~~~ghm~~~~",
        "~~~~~~~~~~~~~~~gh~~~~",
        "~~~~~~~~~~~~~~~~~~~~~"
    };

    internal static ExplorationTerrainSource Sample()
    {
        var catalog = GameDataMgr.Instance.MapCatalog;
        var cells = new Dictionary<TileId, TerrainCell3D>();
        var visible = new HashSet<TileId>();
        var pavilion = Vector2.Zero;
        TileId? gate = null;
        for (int row = 0; row < SampleRows.Length; row++)
        {
            int r = row - 8;
            for (int col = 0; col < SampleRows[row].Length; col++)
            {
                var id = new TileId(col - 10 - (int)Mathf.Floor(r / 2f), r);
                char symbol = SampleRows[row][col];
                var (type, elevation) = symbol switch
                {
                    'g' => ("coast", .13),
                    'f' => ("forest", .26),
                    'b' => ("bamboo_forest", .24),
                    'h' => ("hill", .39),
                    'G' => ("hill", .59),
                    'm' => ("mountain", .65),
                    's' => ("snow_mountain", .83),
                    'i' => ("lake", .13),
                    'r' => ("river", .16),
                    '~' => ("ocean", .02),
                    _ => ("plain", .19)
                };
                var environment = new MapEnvironment { Elevation = elevation, Temperature = symbol == 's' ? .2 : .56 };
                var water = symbol switch
                {
                    '~' => new MapWater { Kind = "ocean", SurfaceLevel = .05 },
                    'i' => new MapWater { Kind = "lake", SurfaceLevel = .13 },
                    'r' => new MapWater { Kind = "river", Width = 1, IsCenterline = true },
                    _ => new MapWater()
                };
                if (symbol == 'r')
                {
                    for (int direction = 0; direction < 6; direction++)
                    {
                        var neighbor = id.Neighbor(direction);
                        int nr = neighbor.R + 8;
                        int nc = neighbor.Q + 10 + (int)Mathf.Floor(neighbor.R / 2f);
                        if (nr < 0 || nr >= SampleRows.Length || nc < 0 || nc >= SampleRows[nr].Length) continue;
                        char next = SampleRows[nr][nc];
                        if (next == 'r' && nr == row - 1) water.IncomingMask |= 1 << direction;
                        if (nr == row + 1 && (next == 'r' || (next == '~' && row == 12)) && water.OutDirection < 0)
                            water.OutDirection = direction;
                    }
                    water.IsSource = row == 8;
                }
                catalog.Tiles.TryGetValue(type, out var define);
                string landmark = symbol switch { 'P' => "听松亭", 'G' => "栖霞山门", _ => "" };
                cells.Add(id, TerrainCell3D.From(environment, water, define, landmark));
                visible.Add(id);
                if (symbol == 'P') pavilion = ExplorationTerrainSurface.Center(id);
                if (symbol == 'G') gate = id;
            }
        }
        return new(2718, -1, visible, cells, new[]
        {
            pavilion + new Vector2(-2.3f, -.95f), pavilion + new Vector2(-1.15f, -.43f),
            pavilion + new Vector2(-.55f, -.12f), pavilion, pavilion + new Vector2(.43f, .16f),
            pavilion + new Vector2(.77f, .51f), pavilion + new Vector2(.53f, 1.21f),
            pavilion + new Vector2(.86f, 1.96f), pavilion + new Vector2(1.72f, 2.45f)
        }, gate);
    }
}

/// <summary>从地域标签和真实水文映射视觉参数，新增地域可沿用现有标签。</summary>
internal readonly record struct TerrainCell3D(string Name, Color Color, float Elevation, float Mountain, float Trees,
    float Snow, MapWater Water, string Landmark)
{
    internal bool StillWater => Water.Kind is "ocean" or "lake";
    internal float WaterHeight => ((float)(Water.SurfaceLevel ?? .05) - .05f) * 1.5f;
    internal float Height => StillWater ? WaterHeight - (Water.Kind == "ocean" ? .68f : .37f)
        : .13f + Elevation * 1.08f + Mountain * (.20f + Elevation * .45f);

    internal static TerrainCell3D From(MapEnvironment environment, MapWater water, TileDefine? define, string landmark)
    {
        bool Has(string tag) => define?.Tags.Contains(tag) == true;
        float mountain = Has("山类") ? 1f : Has("丘陵") ? .27f : Has("高地") ? .35f : 0f;
        float trees = Has("林木") ? 1f : Has("湿润") ? .34f : Has("干旱") || Has("炎热") ? .02f : .16f;
        float snow = Has("寒冷") && mountain >= 1f ? 1f : (float)Mathf.Clamp((.34 - environment.Temperature) * 1.5, 0, .48);
        // 河水沿水文边生成，格内剩余的区域仍是带植被的河岸。
        var color = water.Kind == "river" ? new Color("#9ba66a").SrgbToLinear()
            : Color.FromString(define?.Color ?? "", new Color("#91a16e")).SrgbToLinear();
        return new(define?.DisplayName ?? "地块", color, (float)environment.Elevation, mountain, trees, snow, water, landmark);
    }
}
