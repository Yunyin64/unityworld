using System.Reflection;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.World;

namespace UnityWorld.Tests;

/// <summary>运行时契约测试：候选不公开、初值与速率、跨位面快照和可恢复的发布失败。</summary>
public static class MapRuntimeTests
{
    private static int _assertions;
    private static void Check(bool condition, string message)
    {
        _assertions++;
        if (!condition) throw new InvalidOperationException("地图运行时断言失败：" + message);
    }
    private static void Reject(Action action, string message)
    {
        try { action(); }
        catch (Exception ex) when (ex is InvalidOperationException || ex is InvalidDataException || ex is ArgumentException || ex is MapGenerationException)
        { _assertions++; return; }
        throw new InvalidOperationException("应该拒绝：" + message);
    }
    private static TileAura Fire(float amount) => new() { Jin = 0, Mu = 0, Shui = 0, Huo = amount, Tu = 0 };
    private static bool Same(TileAura a, TileAura b) => Math.Abs(a.Jin - b.Jin) < 0.0001f
        && Math.Abs(a.Mu - b.Mu) < 0.0001f && Math.Abs(a.Shui - b.Shui) < 0.0001f
        && Math.Abs(a.Huo - b.Huo) < 0.0001f && Math.Abs(a.Tu - b.Tu) < 0.0001f;

    public static void Run(string baseDir)
    {
        _assertions = 0;
        PlaneFactoryContracts();
        SnapshotModifiersAndLandmarks();
        CandidateAuraOrder();
        ForcedLandmarkRuntime(baseDir);
        ContinentPublicationRollback(baseDir);
        RuntimeWorld(baseDir);
        Console.WriteLine($"MAP_RUNTIME_OK assertions={_assertions}");
    }

    private static void PlaneFactoryContracts()
    {
        var main = PlaneDefine.MainWorld().ToConfig();
        Check(main.Width == 200 && main.Height == 200 && main.Kind == PlaneTypes.PlaneKind.MainPlane
            && main.GenerationMode == PlaneGenerationMode.Continent && main.GenerationProfileId == "china-inspired-v1"
            && main.GenerationSeed == 12345, "PlaneDefine主大陆工厂与ToConfig使用固定尺寸和完整生成");
        var explicitProfile = PlaneDefine.MainWorld().ToConfig("other-profile", 73);
        Check(explicitProfile.GenerationProfileId == "other-profile" && explicitProfile.GenerationSeed == 73,
            "定义转换显式传递生成配置与种子");
        Reject(() => PlaneDefine.MainWorld(100, 60).ToConfig(), "旧100×60主世界尺寸不再合法");
        Reject(() => new PlaneDefine { ID = "bad-main", Kind = PlaneTypes.PlaneKind.MainPlane }.Validate(), "主大陆缺少尺寸不能自动补小位面默认值");
        var small = new PlaneDefine { ID = "small" }.ToConfig();
        Check(small.Width == 20 && small.Height == 20 && small.GenerationMode == PlaneGenerationMode.BasicFill,
            "小位面定义仍保留20×20默认基础填充");
        var custom = PlaneDefine.SubPlane("custom", "custom", 7, 9).ToConfig();
        Check(custom.Width == 7 && custom.Height == 9 && custom.GenerationMode == PlaneGenerationMode.BasicFill,
            "小位面自定义尺寸不套用主大陆尺寸");
        var mgr = new PlaneMgr();
        Reject(() => mgr.CreatePlane(new PlaneConfig { Kind = PlaneTypes.PlaneKind.MainPlane,
            Width = 200, Height = 200, GenerationMode = PlaneGenerationMode.BasicFill }), "公共创建入口拒绝主位面基础填充");
        var direct = PlaneConfig.ForMainPlane(seed: 73);
        Check(direct.Width == main.Width && direct.Height == main.Height && direct.GenerationMode == main.GenerationMode
            && direct.GenerationProfileId == main.GenerationProfileId && direct.GenerationSeed == 73,
            "两个主大陆工厂采用同一尺寸和生成模式契约");
    }

    private static void SnapshotModifiersAndLandmarks()
    {
        var dao = new AuraDaoMgr();
        var planes = new PlaneMgr(); planes.Init();
        var landmarks = new LandMarkMgr(); landmarks.Init();
        var regions = new RegionMgr(); regions.Init();
        try
        {
            // 单坐标旧查询的数值夹具直接构造内部实体，不让公共主大陆入口为测试降级。
            var a = (Plane)Activator.CreateInstance(typeof(Plane), BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { new PlaneId(0), new PlaneConfig { Kind = PlaneTypes.PlaneKind.MainPlane,
                    Width = 1, Height = 1, InitialTiles = new[] { new Tile(TileId.Zero, Fire(2)) } } }, null);
            dao.TakeSnapshot(a);
            var b = planes.CreatePlane(new PlaneConfig { Width = 1, Height = 1,
                InitialTiles = new[] { new Tile(TileId.Zero, Fire(7)) } });
            Check(dao.GetOrigin(TileId.Zero).Huo == 2 && dao.GetOrigin(b.Id, TileId.Zero).Huo == 7, "同坐标按位面隔离，旧查询只查主世界");
            dao.GetOrigin(b.Id, TileId.Zero).Huo = 1000;
            Check(dao.GetOrigin(b.Id, TileId.Zero).Huo == 7, "读取返回独立副本");
            a.GetTile(TileId.Zero).CurrentAura.Huo = 5;
            b.GetTile(TileId.Zero).CurrentAura.Huo = 6;
            Check(dao.CalculateBalance()[BaseElementType.Huo] == 2, "两位面差额分别为3和-1");
            Reject(() => dao.TakeSnapshot(a), "已有快照不可覆盖");
            Check(dao.GetOrigin(a.Id, TileId.Zero).Huo == 2, "重复登记失败保留旧原初值");

            var source = Fire(5);
            var finite = new TileModifier("rate", "natural-1", source, 2);
            var permanent = new TileModifier("rate", "other-source", source);
            source.Huo = 500;
            Check(finite.Id == "rate" && finite.SourceId == "natural-1" && finite.RemainingTime == 2 && finite.AuraData.Huo == 5, "修正身份、来源、时长和速率深拷贝");
            finite.AuraData.Huo = 6;
            Check(permanent.AuraData.Huo == 5, "实例之间速率独立");
            finite.AuraData.Huo = 5;
            var tile = b.GetTile(TileId.Zero); tile.Modifiers.Add(finite);
            var auraSystem = new TileSystemAura();
            auraSystem.Tick(b, 0.5f); auraSystem.Tick(b, 1.5f);
            Check(tile.CurrentAura.Huo == 16 && tile.Modifiers.Count == 0, "分段deltaTime累积且到期移除");
            tile.Modifiers.Add(permanent); auraSystem.Tick(b, 0.25f);
            Check(tile.CurrentAura.Huo == 17.25f && tile.Modifiers.Count == 1, "永久修正不自动过期");

            var catalog = new MapCatalog();
            catalog.Modifiers.Add("rate", new TileModifierDefine { ID = "rate", AuraData = Fire(2) });
            catalog.LandMarks.Add("natural", new LandMarkDefine { ID = "natural", IsSingleton = true,
                AuraOffset = Fire(3), ModifierDefineIds = new() { "rate" } });
            catalog.LandMarks.Add("broken", new LandMarkDefine { ID = "broken", ModifierDefineIds = new() { "rate", "missing" } });
            landmarks.SetCatalog(catalog);
            int modifiersBefore = a.GetTile(TileId.Zero).Modifiers.Count;
            Check(landmarks.Place(a, a.GetTile(TileId.Zero), "broken") == null, "缺失修正引用拒绝放置");
            Check(a.GetTile(TileId.Zero).Modifiers.Count == modifiersBefore && a.GetTile(TileId.Zero).LandMarkId == null, "失败不部分挂载");
            var placed = landmarks.Place(a, a.GetTile(TileId.Zero), "natural");
            Check(placed != null && a.GetTile(TileId.Zero).LandMarkId == placed.Id, "地标双向关联");
            Check(a.GetTile(TileId.Zero).CurrentAura.Huo == 5 && dao.GetOrigin(TileId.Zero).Huo == 2, "晚生自然地标不瞬间改浓度、不改原初");
            auraSystem.Tick(a, .5f);
            Check(a.GetTile(TileId.Zero).CurrentAura.Huo == 7.5f && dao.GetOrigin(TileId.Zero).Huo == 2,
                "晚生自然地标偏移3与兼容修正2均按秒累积");
            Check(landmarks.Place(b, tile, "natural") == null && tile.Modifiers.Count == 1, "singleton跨位面约束");
            Check(landmarks.Place(a, a.GetTile(TileId.Zero), "natural") == null, "一个格子不能放两个地标");
            a.GetTile(TileId.Zero).AddModifier(new TileModifier("other", "unrelated", Fire(1)));
            landmarks.Remove(placed.Id);
            Check(a.GetTile(TileId.Zero).LandMarkId == null && a.GetTile(TileId.Zero).Modifiers.Count == 1
                && a.GetTile(TileId.Zero).CurrentAura.Huo == 7.5f, "移除只清理自身来源，不回滚历史浓度");
            auraSystem.Tick(a, .5f);
            Check(a.GetTile(TileId.Zero).CurrentAura.Huo == 8, "移除地标后偏移速率停止，其他来源继续变化");

            catalog.Tiles.Add("plain", new TileDefine { ID = "plain", Tags = new() { "可刷新" } });
            catalog.LandMarks.Add("corpse", new LandMarkDefine { ID = "corpse", IsNatural = false,
                PlacementTypeIds = new() { "plain" }, PlacementTags = new() { "可刷新" }, SpawnWeight = 0, AuraOffset = Fire(-2) });
            var candidates = landmarks.GetIndependentSpawnCandidates(b, "corpse");
            Check(candidates.Count == 1 && ReferenceEquals(candidates[0], tile), "独立刷新按类型标签找格，不依赖地区和创世权重");
            var corpse = landmarks.Place(b, candidates[0], "corpse");
            Check(tile.CurrentAura.Huo == 17.25f && !landmarks.GetIndependentSpawnCandidates(b, "corpse").Any(), "人工地标放置无瞬间变化，已占格不再成为候选");
            auraSystem.Tick(b, .5f);
            Check(tile.CurrentAura.Huo == 18.75f && dao.GetOrigin(b.Id, tile.Id).Huo == 7, "人工负偏移与其他来源分别累积，原初不变");
            landmarks.Remove(corpse.Id);

            int originalCount = planes.Count;
            foreach (string stage in new[] { "prepared", "regions", "landmarks", "snapshot" })
            {
                planes.PublicationFailureStageForTesting = stage;
                Reject(() => planes.CreatePlane(new PlaneConfig { Width = 2, Height = 2 }), "发布故障 " + stage);
                Check(planes.Count == originalCount && dao.GetOrigin(TileId.Zero).Huo == 2, "发布失败保留已有位面和快照");
            }
            planes.PublicationFailureStageForTesting = null;
            var retry = planes.CreatePlane(new PlaneConfig { Width = 2, Height = 2 });
            Check(retry.TileCount == 4, "失败后可再次创建");
            planes.Destroy(retry.Id);
            planes.Destroy(b.Id);
            Check(dao.GetOrigin(b.Id, TileId.Zero) == null && dao.CalculateBalance()[BaseElementType.Huo] == 6, "销毁只注销自己的快照和收支");
            var empty = planes.CreateSubPlane("empty");
            Check(empty.TileCount == 0 && empty.Width == 0, "显式基础填充保留空位面");
        }
        finally { planes.End(); landmarks.End(); regions.End(); dao.End(); }
        Check(PlaneMgr.Instance == null && LandMarkMgr.Instance == null && RegionMgr.Instance == null && dao.GetOrigin(TileId.Zero) == null, "End清空查询与原初");
    }

    private static void CandidateAuraOrder()
    {
        var catalog = new MapCatalog();
        catalog.Tiles.Add("mountain", new TileDefine { ID = "mountain", InitialAura = Fire(1) });
        catalog.Regions.Add("range", new RegionDefine { ID = "range",
            TerrainLayout = new() { [(0, 0)] = "mountain", [(2, 0)] = "mountain" } });
        catalog.Modifiers.Add("rate", new TileModifierDefine { ID = "rate", AuraData = Fire(5) });
        catalog.LandMarks.Add("nature", new LandMarkDefine { ID = "nature", AuraOffset = Fire(3), ModifierDefineIds = new() { "rate" } });
        catalog.LandMarks.Add("building", new LandMarkDefine { ID = "building", IsNatural = false, AuraOffset = Fire(4) });
        var generated = new GeneratedWorld { Width = 4, Height = 1, Cells = new[] {
            new GeneratedCell { Col = 0, TileDefineId = "mountain", Source = TileGenerationSource.Region, RegionIndex = 0, LandMarkDefineId = "nature" },
            new GeneratedCell { Col = 1, TileDefineId = "mountain", Source = TileGenerationSource.Fill, LandMarkDefineId = "building" },
            new GeneratedCell { Col = 2, TileDefineId = "mountain", Source = TileGenerationSource.Region, RegionIndex = 0, LandMarkDefineId = "building" },
            new GeneratedCell { Col = 3, TileDefineId = "mountain", Source = TileGenerationSource.Fill, LandMarkDefineId = "nature" } },
            Regions = new() { new GeneratedRegion { Index = 0, DefineId = "range", Anchor = TileId.Zero, CellIndices = new() { 0, 2 } } } };
        var plane = (Plane)Activator.CreateInstance(typeof(Plane), BindingFlags.Instance | BindingFlags.NonPublic,
            null, new object[] { new PlaneId(77), new PlaneConfig { Width = 4, Height = 1 } }, null);
        var regions = new RegionMgr(); regions.Init();
        var landmarks = new LandMarkMgr(); landmarks.Init(); landmarks.SetCatalog(catalog);
        var dao = new AuraDaoMgr();
        try
        {
            // 对内部候选构建边界做数值夹具，无需把测试专用发布API暴露给游戏。
            var content = typeof(PlaneGenerator).GetMethod("BuildContinent", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { plane, catalog, generated, regions, landmarks });
            var snapshot = (Dictionary<TileId, TileAura>)content.GetType().GetProperty("NaturalSnapshot").GetValue(content);
            var plannedLandmarks = (List<LandMark>)content.GetType().GetProperty("LandMarks").GetValue(content);
            Check(plannedLandmarks.Select(lm => catalog.LandMarks[lm.DefineId].IsNatural).SequenceEqual(new[] { true, true, false, false }),
                "地区附带与独立地标统一分组，全部自然实体先创建、人工后创建");
            Check(snapshot[new TileId(3, 0)].Huo == 4 && snapshot[new TileId(1, 0)].Huo == 1,
                "独立自然地标计入快照，独立人工地标不计入");
            Check(regions.Count == 0 && landmarks.Count == 0 && !dao.HasSnapshot(plane.Id), "候选实体与准备快照不公开");
            Check(snapshot[new TileId(0, 0)].Huo == 4 && snapshot[new TileId(2, 0)].Huo == 1, "自然偏移进入快照，人工偏移不进入");
            Check(plane.GetTile(new TileId(0, 0)).CurrentAura.Huo == 4 && plane.GetTile(new TileId(2, 0)).CurrentAura.Huo == 1, "创世人工地标生成时不立即改变浓度");
            Check(plane.GetTile(new TileId(1, 0)).RegionId == null, "孔洞无地区归属");
            Check(catalog.Tiles["mountain"].InitialAura.Huo == 1, "实体元气未修改定义");
            dao.RegisterSnapshot(plane, snapshot);
            snapshot[new TileId(0, 0)].Huo = 999;
            Check(dao.GetOrigin(plane.Id, TileId.Zero).Huo == 4 && dao.CalculateBalance()[BaseElementType.Huo] == 0, "发布时当前值等于创世快照，人工影响尚未累积");
            new TileSystemAura().Tick(plane, 0.5f);
            Check(plane.GetTile(TileId.Zero).CurrentAura.Huo == 6.5f && dao.GetOrigin(plane.Id, TileId.Zero).Huo == 4, "创世自然偏移不再重复，兼容modifier按秒累积");
            Check(plane.GetTile(new TileId(2, 0)).CurrentAura.Huo == 3 && dao.GetOrigin(plane.Id, new TileId(2, 0)).Huo == 1,
                "快照后人工地标的偏移4在半秒后增加2");
        }
        finally { regions.End(); landmarks.End(); dao.End(); }
    }

    private static void ForcedLandmarkRuntime(string baseDir)
    {
        var log = new LogMgr(41);
        GameDataMgr.Initialize(baseDir);
        var catalog = GameDataMgr.Instance.MapCatalog;
        catalog.LandMarks.Add("forced-origin", new() { ID = "forced-origin", SpawnWeight = 0, IsSingleton = true,
            AuraOffset = Fire(3), PlacementTypeIds = new() { "ocean" }, ForcedPositions = new() { new() { Col = 0, Row = 0 } } });
        catalog.LandMarks.Add("forced-building", new() { ID = "forced-building", IsNatural = false, SpawnWeight = 0,
            AuraOffset = Fire(4), PlacementTypeIds = new() { "ocean" }, ForcedPositions = new() { new() { Col = 90, Row = 80 } } });
        var dao = new AuraDaoMgr();
        var planes = new PlaneMgr(); planes.Init();
        var tiles = new TileMgr(); tiles.Init();
        var regions = new RegionMgr(); regions.Init();
        var landmarks = new LandMarkMgr(); landmarks.Init(); landmarks.SetCatalog(catalog);
        try
        {
            var options = new WorldGenerationOptions { RegionCoverageTarget = 0, OptionalRegionAttempts = 0,
                LandmarkDensity = 0, Hydrology = new() { RiverCount = 0 } };
            var plane = planes.CreatePlane(new PlaneConfig { Width = 200, Height = 200,
                GenerationMode = PlaneGenerationMode.Continent, GenerationOptions = options });
            var natural = plane.GetTile(Plane.OffsetToAxial(0, 0));
            var artificial = plane.GetTile(Plane.OffsetToAxial(90, 80));
            Check(natural.IsFixed && natural.HasForcedLandmark && artificial.HasForcedLandmark && !artificial.IsFixed,
                "固定地标位置与固定地区来源彼此独立，均复制到运行时");
            float naturalBase = catalog.Tiles[natural.TypeId].InitialAura.Huo;
            float artificialBase = catalog.Tiles[artificial.TypeId].InitialAura.Huo;
            Check(natural.CurrentAura.Huo == naturalBase + 3 && dao.GetOrigin(plane.Id, natural.Id).Huo == naturalBase + 3,
                "绝对坐标自然地标在快照之前立即应用");
            Check(artificial.CurrentAura.Huo == artificialBase && dao.GetOrigin(plane.Id, artificial.Id).Huo == artificialBase,
                "绝对坐标人工地标在快照后创建，没有瞬间增量");
            Check(regions.GetAt(plane.Id, natural.Id).LandMarkIds.Contains(natural.LandMarkId.Value), "单独固定地标也建立所在地地区关联");
            new TileSystemAura().Tick(plane, .5f);
            Check(natural.CurrentAura.Huo == naturalBase + 3 && artificial.CurrentAura.Huo == artificialBase + 2,
                "创世自然偏移不重复，固定人工地标在半秒后增加2");
            Check(plane.GenerationResult.ConfigHash == catalog.ComputeConfigHash(options), "PlaneConfig普通参数进入有效配置摘要");
        }
        finally { planes.End(); landmarks.End(); regions.End(); tiles.End(); dao.End(); log.End(); }
    }

    private static void RuntimeWorld(string baseDir)
    {
        WorldMgr.Initialize(12345, baseDir);
        try
        {
            var plane = PlaneMgr.Instance.MainPlane;
            var catalog = GameDataMgr.Instance.MapCatalog;
            Check(TileDefineMgr.Instance.GetAll().Count() == catalog.Tiles.Count
                && RegionDefineMgr.Instance.GetAll().Count() == catalog.Regions.Count
                && LandMarkDefineMgr.Instance.GetAll().Count() == catalog.LandMarks.Count, "分拆定义目录与兼容管理器加载一致");
            Check(plane.TileCount == 40000 && plane.GenerationResult.Cells.Length == 40000, "默认入口实际生成完整大陆");
            foreach(var plan in plane.GenerationResult.Cells.Where(c=>c.Water.Kind!="none"))
            {
                var actual=plane.GetTile(plan.Id);var projection=actual.Water;
                Check(projection.Kind==plan.Water.Kind && projection.BodyId==plan.Water.BodyId && projection.Width==plan.Water.Width
                    && projection.SurfaceLevel==plan.Water.SurfaceLevel && projection.Runoff==plan.Water.Runoff
                    && projection.DrainageElevation==plan.Water.DrainageElevation, "水文投影逐格一致");
                projection.Runoff=-1;projection.BodyId="tampered";
                Check(actual.Water.Runoff==plan.Water.Runoff && actual.Water.BodyId==plan.Water.BodyId, "只读投影不能回写世界或候选");
            }
            Check(!PlaneMgr.Instance.Destroy(plane.Id), "已发布的真实主大陆不能由Destroy销毁");
            Check(WorldTime.CurrentTick == 0 && WorldTime.TotalTime == 0, "地图生成不推进时间");
            // 周历也只能在世界成功建立后初始化，生成本身不消耗游戏周数。
            Check(WorldTime.CurrentDate.ElapsedWeeks == 0 && WorldTime.CurrentDate.PeriodKind == WorldPeriodKind.NewYear,
                "首次世界发布建立4000年新年，不预先推进周历");
            Check(RegionMgr.Instance.GetByPlane(plane.Id).Count() == plane.GenerationResult.Regions.Count, "首次启动地区索引已经可用");
            foreach (var region in RegionMgr.Instance.GetByPlane(plane.Id))
            {
                var define = catalog.Regions[region.DefineId];
                Check(region.OwnedTiles.SetEquals(define.TerrainLayout.Keys.Select(k => region.Anchor + new TileId(k.dq, k.dr))), "实际地区足迹等于固定模板");
                Check(region.OwnedTiles.All(t => plane.GetTile(t).RegionId == region.Id
                    && RegionMgr.Instance.GetAt(plane.Id, t) == region), "地块地区双向关联");
                Check(TileMgr.Instance.GetByRegion(region.Id).Select(t => t.Id).ToHashSet().SetEquals(region.OwnedTiles), "地区地块查询只返回实际占地");
                Check(region.LandMarkIds.Count == define.LandMarkLayout.Count && region.LandMarkIds.All(id =>
                {
                    var landmark = LandMarkMgr.Instance.GetById(id);
                    var relative = landmark.Position - region.Anchor;
                    return define.LandMarkLayout.TryGetValue((relative.Q, relative.R), out var definitionId)
                        && definitionId == landmark.DefineId && landmark.PlaneId == region.PlaneId;
                }), "地区地标列表与模板布局逐项对应");
            }
            bool auraValid = true;
            foreach (var tile in plane.Tiles.Values)
            {
                var type = catalog.Tiles[tile.TypeId];
                var expected = new TileAura { Jin = type.InitialAura.Jin, Mu = type.InitialAura.Mu,
                    Shui = type.InitialAura.Shui, Huo = type.InitialAura.Huo, Tu = type.InitialAura.Tu };
                if (tile.RegionId is int id) expected.AddFrom(catalog.Regions[RegionMgr.Instance.GetById(id).DefineId].AuraProfile);
                LandMarkDefine landmark = tile.LandMarkId.HasValue ? catalog.LandMarks[LandMarkMgr.Instance.GetById(tile.LandMarkId.Value).DefineId] : null;
                if (landmark?.IsNatural == true) expected.AddFrom(landmark.AuraOffset);
                auraValid &= Same(WorldMgr.AuraDaoMgr.GetOrigin(plane.Id, tile.Id), expected);
                auraValid &= Same(tile.CurrentAura, expected);
            }
            Check(auraValid, "四万格初值与快照顺序逐格吻合");
            var typeId = plane.Tiles.Values.First().TypeId;
            Check(TileMgr.Instance.GetByType(plane.Id, typeId).All(t => t.TypeId == typeId), "类型查询不自动扩大到变体");
            Check(TileMgr.Instance.GetTile(plane.Id, TileId.Zero) == plane.GetTile(TileId.Zero), "跨位面地块查询已注册");
            Check(TileMgr.Instance.GetWithNoRegion(plane.Id).Count() == plane.Tiles.Values.Count(t => t.RegionId == null)
                && plane.GenerationResult.Cells.Any(cell => cell.IsFixed)
                && TileMgr.Instance.GetBySource(plane.Id, TileGenerationSource.Fill).Any(), "无地区与来源查询各自保留语义");
            Check(!TileMgr.Instance.GetByRegion(-1).Any() && !TileMgr.Instance.GetWithNoRegion(-1).Any()
                && !TileMgr.Instance.GetBySource(-1, TileGenerationSource.Fill).Any(), "未知位面地区返回空结果");
            var tag = catalog.Tiles.Values.SelectMany(t => t.Tags).First();
            Check(TileMgr.Instance.GetByTag(plane.Id, tag).All(t => catalog.Tiles[t.TypeId].Tags.Contains(tag)), "标签查询使用定义标签");
            var mainOrigin = WorldMgr.AuraDaoMgr.GetOrigin(TileId.Zero);
            WorldTime.AdvanceWeek();
            var dateBeforeSubPlane = WorldTime.CurrentDate;
            var existing = new Tile(TileId.Zero, Fire(17));
            var sub = PlaneMgr.Instance.CreatePlane(new PlaneConfig { Width = 2, Height = 2, InitialTiles = new[] { existing } });
            Check(ReferenceEquals(sub.GetTile(TileId.Zero), existing) && sub.TileCount == 4 && existing.CurrentAura.Huo == 17, "基础填充保留手工格内容并补齐");
            Check(Same(mainOrigin, WorldMgr.AuraDaoMgr.GetOrigin(TileId.Zero)), "小位面未覆盖主世界原初");
            Check(RegionMgr.Instance.GetAt(sub.Id, TileId.Zero) == null
                && RegionMgr.Instance.GetByPlane(sub.Id).Count() == 0, "跨位面同坐标不借用主世界地区");
            PlaneMgr.Instance.Destroy(sub.Id);
            Check(WorldMgr.AuraDaoMgr.GetOrigin(sub.Id, TileId.Zero) == null, "销毁清理小位面原初");
            Check(WorldTime.CurrentDate == dateBeforeSubPlane, "创建和销毁额外位面不改变已有周历");
            Reject(() => PlaneMgr.Instance.CreatePlane(new PlaneConfig { Width = 2, Height = 2,
                GenerationMode = PlaneGenerationMode.Continent }), "非200尺寸大陆不能降级");
            var regionWithLandmark = RegionMgr.Instance.GetByPlane(plane.Id).First(r => r.LandMarkIds.Count != 0);
            int landmarkId = regionWithLandmark.LandMarkIds[0];
            var landmarkTile = LandMarkMgr.Instance.GetById(landmarkId).Position;
            LandMarkMgr.Instance.Remove(landmarkId);
            Check(!regionWithLandmark.LandMarkIds.Contains(landmarkId) && plane.GetTile(landmarkTile).LandMarkId == null,
                "移除地标同步更新地区列表和地块关联");
        }
        finally { WorldMgr.End(); }
        WorldTime.Advance(2);
        // 使用非初始日期检查失败启动：不能通过意外重置到0周碰巧通过断言。
        WorldTime.AdvanceWeek();
        var previousCalendar = WorldTime.Calendar;
        var previousDate = WorldTime.CurrentDate;
        int oldWeekNotifications = 0;
        previousCalendar.WeekAdvanced += _ => oldWeekNotifications++;
        int previousTick = WorldTime.CurrentTick;
        float previousTime = WorldTime.TotalTime;
        Reject(() => WorldMgr.Initialize(12345, baseDir, "missing-profile"), "启动失败不生成空白大陆");
        Check(WorldTime.CurrentTick == previousTick && WorldTime.TotalTime == previousTime
            && PlaneMgr.Instance == null && RegionMgr.Instance == null && LandMarkMgr.Instance == null,
            "失败启动不改时间且释放地图管理器，可再次重试");
        Check(ReferenceEquals(WorldTime.Calendar, previousCalendar) && WorldTime.CurrentDate == previousDate,
            "失败启动保留原周历实例、日期和订阅");
        WorldMgr.Initialize(12345, baseDir);
        try
        {
            Check(PlaneMgr.Instance.Count == 1 && WorldTime.CurrentTick == 0, "同进程世界重启没有旧位面和时间");
            Check(WorldTime.CurrentDate.ElapsedWeeks == 0 && !ReferenceEquals(WorldTime.Calendar, previousCalendar),
                "成功重启从新年建立新周历");
            WorldTime.AdvanceWeek();
            Check(oldWeekNotifications == 0, "世界成功重启后旧订阅不泄漏到新世界");
        }
        finally { WorldMgr.End(); }
    }

    private static void ContinentPublicationRollback(string baseDir)
    {
        var log = new LogMgr(12345);
        GameDataMgr.Initialize(baseDir);
        var dao = new AuraDaoMgr();
        var planes = new PlaneMgr(); planes.Init();
        var tiles = new TileMgr(); tiles.Init();
        var regions = new RegionMgr(); regions.Init();
        var landmarks = new LandMarkMgr(); landmarks.Init(); landmarks.SetCatalog(GameDataMgr.Instance.MapCatalog);
        try
        {
            var baseline = planes.CreatePlane(new PlaneConfig { Width = 1, Height = 1,
                InitialTiles = new[] { new Tile(TileId.Zero, Fire(2)) } });
            var config = new PlaneConfig { Name = "事务测试大陆", Width = 200, Height = 200,
                GenerationMode = PlaneGenerationMode.Continent, GenerationSeed = 12345 };
            int tick = WorldTime.CurrentTick;
            float time = WorldTime.TotalTime;
            WorldTime.AdvanceWeek();
            var dateBeforeCandidate = WorldTime.CurrentDate;
            var lakeLandmark=GameDataMgr.Instance.MapCatalog.LandMarks["natural_reed_marsh"];
            bool singletonBefore=lakeLandmark.IsSingleton;lakeLandmark.IsSingleton=true;
            try { Reject(()=>planes.CreatePlane(config), "必需三湖singleton冲突在候选阶段失败"); }
            finally {lakeLandmark.IsSingleton=singletonBefore;}
            Check(planes.Count==1 && regions.Count==0 && landmarks.Count==0 && dao.GetOrigin(baseline.Id,TileId.Zero).Huo==2, "三湖失败不覆盖历史世界与快照");
            planes.PublicationFailureStageForTesting = "snapshot";
            Reject(() => planes.CreatePlane(config), "所有地区地标及快照登记后注入失败");
            Check(planes.Count == 1 && regions.Count == 0 && landmarks.Count == 0
                && dao.GetOrigin(baseline.Id, TileId.Zero).Huo == 2 && dao.CalculateBalance()[BaseElementType.Huo] == 0,
                "晚期失败撤销地区、地标、单例与快照，保留旧位面");
            Check(WorldTime.CurrentTick == tick && WorldTime.TotalTime == time, "晚期失败不改变世界时间");
            Check(WorldTime.CurrentDate == dateBeforeCandidate, "候选校验和晚期发布失败均保留已有周历");
            planes.PublicationFailureStageForTesting = null;
            var success = planes.CreatePlane(config);
            var placedLandmarks = landmarks.GetByPlane(success.Id).ToArray();
            Check(success.TileCount == 40000 && regions.Count > 0 && placedLandmarks.Length > 0,
                "相同生成请求在失败回滚后可成功发布");
            var lakePlans=success.GenerationResult.Regions.Where(r=>GameDataMgr.Instance.MapCatalog.Regions[r.DefineId].GenerationPhase=="lake").SelectMany(r=>r.LandMarkCellIndices).Select(i=>success.GenerationResult.Cells[i]).ToArray();
            Check(lakePlans.Any(c=>GameDataMgr.Instance.MapCatalog.LandMarks[c.LandMarkDefineId].IsNatural) && lakePlans.Any(c=>!GameDataMgr.Instance.MapCatalog.LandMarks[c.LandMarkDefineId].IsNatural), "自然与人工湖泊地标统一进入既有创世阶段");
            var parallel=planes.CreatePlane(new PlaneConfig {Width=200,Height=200,GenerationMode=PlaneGenerationMode.Continent,GenerationSeed=19});
            var waterCell=success.GenerationResult.Cells.First(c=>c.Water.Kind=="lake");var oldWater=success.GetTile(waterCell.Id).Water;
            var other=parallel.GetTile(waterCell.Id).Water;other.BodyId="mutated-other";other.Runoff=-9;
            Check(success.GetTile(waterCell.Id).Water.BodyId==oldWater.BodyId && parallel.GetTile(waterCell.Id).Water.Runoff>=0 && dao.GetOrigin(baseline.Id,TileId.Zero).Huo==2, "两座大陆水文与旧位面快照隔离");
            planes.Destroy(parallel.Id);
            var position = success.GenerationResult.Regions[0].Anchor;
            planes.Destroy(success.Id);
            Check(WorldTime.CurrentDate == dateBeforeCandidate, "候选成功发布和销毁也不推进世界周历");
            Check(planes.Count == 1 && regions.Count == 0 && landmarks.Count == 0
                && placedLandmarks.All(lm => !lm.IsActive) && regions.GetAt(success.Id, position) == null
                && dao.GetOrigin(success.Id, TileId.Zero) == null && dao.GetOrigin(baseline.Id, TileId.Zero).Huo == 2,
                "销毁带地区地标的位面清除全部索引而保留其他位面");
        }
        finally { planes.End(); landmarks.End(); regions.End(); tiles.End(); dao.End(); log.End(); }
    }
}
