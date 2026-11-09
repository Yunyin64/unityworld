
using System.Collections;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 位面管理器：负责位面的创建、销毁、查询，以及驱动各位面Tick
    /// </summary>
    public class PlaneMgr:IDomainMgrBase
    {
        // ── 数据 ─────────────────────────────────────────

        private readonly Dictionary<PlaneId, Plane> _planes = new();

        public static PlaneMgr Instance { get; private set; } 

        /// <summary>主世界（唯一，游戏初始化时创建）</summary>
        public Plane MainPlane { get; private set; }

        // ── 初始化 ────────────────────────────────────────

        /// <summary>
        /// 初始化查询入口；所有地图依赖就绪后再调用 CreateMainPlane。
        /// </summary>
        public void Init()
        {
            Instance  = this;
        }

        /// <summary>仅在地图依赖全部 Init 后显式创建，避免依赖注册列表的偶然顺序。</summary>
        public Plane CreateMainPlane(int seed = 12345, string profileId = "china-inspired-v1")
            => CreatePlane(PlaneConfig.ForMainPlane("主世界", profileId, seed));

        /// <summary>诊断测试注入发布阶段失败，生产保持 null。</summary>
        public string PublicationFailureStageForTesting { get; set; }

        // ── 创建 ─────────────────────────────────────────

        /// <summary>
        /// 通用位面创建入口：接受完整的 <see cref="PlaneConfig"/> 参数，
        /// 生成唯一ID后构造并注册位面。
        /// </summary>
        /// <param name="config">位面配置，含名称、类型、时间流速、元气加成、初始地块等</param>
        /// <returns>新建的位面实例</returns>
        public Plane CreatePlane(PlaneConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);
            if (!Enum.IsDefined(config.GenerationMode)) throw new ArgumentException("未知位面生成模式。");
            if (config.Kind == PlaneTypes.PlaneKind.MainPlane && config.GenerationMode != PlaneGenerationMode.Continent)
                throw new ArgumentException("主位面必须使用完整大陆生成模式，不允许基础填充降级。");
            if (config.Width < 0 || config.Height < 0 || (config.Width == 0) != (config.Height == 0))
                throw new ArgumentException("位面尺寸须同为零或同为正数。");
            var id = config.Kind == PlaneTypes.PlaneKind.MainPlane ? new PlaneId(0) : PlaneIdGenerator.Generate();
            if (_planes.ContainsKey(id)) throw new InvalidOperationException($"位面 {id} 已存在。");
            var aura = AuraDaoMgr.Instance ?? throw new InvalidOperationException("缺少 AuraDaoMgr。");
            if (aura.HasSnapshot(id)) throw new InvalidOperationException($"位面 {id} 已登记快照。");
            var plane = new Plane(id, config);
            PlaneGenerator.PreparedContent prepared;
            if (config.GenerationMode == PlaneGenerationMode.Continent)
            {
                if (config.Width != 200 || config.Height != 200 || config.InitialTiles != null)
                    throw new ArgumentException("大陆生成固定 200×200，不能混用 InitialTiles 恢复。");
                var catalog = GameDataMgr.Instance?.MapCatalog ?? throw new InvalidOperationException("缺少严格地图定义库。");
                var regions = RegionMgr.Instance ?? throw new InvalidOperationException("缺少 RegionMgr。");
                var landmarks = LandMarkMgr.Instance ?? throw new InvalidOperationException("缺少 LandMarkMgr。");
                if (TileMgr.Instance == null) throw new InvalidOperationException("缺少 TileMgr。");
                var profile = config.GenerationOptions;
                if (profile == null && !catalog.Profiles.TryGetValue(config.GenerationProfileId, out profile))
                    throw new InvalidDataException($"缺少大陆配置 {config.GenerationProfileId}");
                catalog.Validate();
                landmarks.SetCatalog(catalog);
                var generated = WorldMapGenerator.Generate(catalog, profile, config.GenerationSeed, landmarks.ReservedSingletons);
                prepared = PlaneGenerator.BuildContinent(plane, catalog, generated, regions, landmarks);
            }
            else
            {
                PlaneGenerator.Fill(plane);
                prepared = new PlaneGenerator.PreparedContent { NaturalSnapshot = AuraDaoMgr.PrepareSnapshot(plane) };
            }

            // 候选不在任何公开容器内。同步发布失败时撤销本次新增，保留其他位面。
            bool snapshotRegistered = false;
            try
            {
                FailIfRequested("prepared");
                foreach (var region in prepared.Regions) RegionMgr.Instance.Publish(region);
                FailIfRequested("regions");
                foreach (var landmark in prepared.LandMarks)
                    LandMarkMgr.Instance.Publish(landmark, plane.GetTile(landmark.Position));
                FailIfRequested("landmarks");
                aura.RegisterSnapshot(plane, prepared.NaturalSnapshot);
                snapshotRegistered = true;
                FailIfRequested("snapshot");
                _planes.Add(id, plane);
                if (config.Kind == PlaneTypes.PlaneKind.MainPlane) MainPlane = plane;
            }
            catch
            {
                if (snapshotRegistered) aura.UnregisterPlane(id);
                LandMarkMgr.Instance?.RemovePlane(id);
                RegionMgr.Instance?.RemovePlane(id);
                _planes.Remove(id);
                throw;
            }
            return plane;
        }

        private void FailIfRequested(string stage)
        {
            if (PublicationFailureStageForTesting == stage)
                throw new InvalidOperationException($"地图发布故障注入：{stage}");
        }

        /// <summary>
        /// 快捷方式：以最简参数创建一个小世界。
        /// </summary>
        /// <param name="name">小世界名称</param>
        /// <param name="timeFlowRate">时间流速倍率（默认 1.0）</param>
        /// <param name="auraBonus">位面元气加成（可选）</param>
        public Plane CreateSubPlane(string name, float timeFlowRate = 1f, TileAura? auraBonus = null)
            => CreatePlane(PlaneConfig.ForSubPlane(name, timeFlowRate, auraBonus));

        // ── 销毁 ─────────────────────────────────────────

        /// <summary>
        /// 销毁指定位面（主世界不可销毁）。
        /// 位面内NPC实体保留；解除其落地与未完成移动，之后可重新放置。
        /// </summary>
        public bool Destroy(PlaneId id)
        {
            UnityWorld.Game.World.WorldMgr.EnsureMutable();
            if (!_planes.TryGetValue(id, out var plane)) return false;
            if (plane.Kind == PlaneTypes.PlaneKind.MainPlane) return false; // 主世界不可销毁

            plane.NpcSystem.ClearMembers();
            plane.MarkDestroyed();
            LandMarkMgr.Instance?.RemovePlane(id);
            RegionMgr.Instance?.RemovePlane(id);
            AuraDaoMgr.Instance?.UnregisterPlane(id);
            _planes.Remove(id);
            return true;
        }

        // ── 查询 ─────────────────────────────────────────

        /// <summary>根据ID获取位面</summary>
        public Plane? GetPlaneById(PlaneId id)
            => _planes.TryGetValue(id, out var p) ? p : null;

        /// <summary>获取所有活跃位面</summary>
        public IEnumerable<Plane> GetAllActive()
            => _planes.Values.Where(p => p.State == PlaneTypes.PlaneState.Active);

        /// <summary>获取所有位面（含封印）</summary>
        public IEnumerable<Plane> GetAll() => _planes.Values;

        /// <summary>当前位面总数（含封印）</summary>
        public int Count => _planes.Count;

        public string Name => "PlaneMgr";

        public string Desc => "";

        // ── NPC归属 ───────────────────────────────────────

        /// <summary>
        /// 查找某NPC当前所在的位面（线性查找，仅适合低频调用）
        /// </summary>
        public Plane? FindPlaneOfNpc(int npcId)
        {
            foreach (var plane in _planes.Values)
                if (plane.ContainsNpc(npcId)) return plane;
            return null;
        }

        /// <summary>
        /// 将NPC从旧位面迁移到新位面（原子操作）。
        /// 同时更新 NpcSystemPosition 里的位置记录（可选传入）。
        /// </summary>
        public bool MoveNpc(int npcId, PlaneId targetPlaneId,
                            TileId targetTileId = default,
                            NpcSystemPosition? posSystem = null)
        {
            // 兼容入口也必须写回真实坐标，不能只移动成员列表。
            var owner = NpcMgr.Instance?.PositionSystem;
            if (owner == null || posSystem != null && !ReferenceEquals(owner, posSystem)) return false;
            return owner.TryPlace(npcId, targetPlaneId.Value, targetTileId);
        }

        // ── Tick ─────────────────────────────────────────

        /// <summary>
        /// 驱动所有活跃位面Tick（预留扩展点，当前位面无独立逻辑）
        /// </summary>
        public void Tick(float deltaTime)
        {
            // 后续位面可有自己的时间流速、天气、事件等系统
            foreach (var plane in GetAllActive())
            {
                // plane.Tick(deltaTime * plane.TimeFlowRate); // 预留：独立时间流速
            }
        }

        public void Begin()
        {
             
        }

        public void Update()
        {
             
        }

        public void Render(float dt)
        {
             
        }

        public void End()
        {
            foreach (var plane in _planes.Values.ToArray())
            {
                LandMarkMgr.Instance?.RemovePlane(plane.Id);
                RegionMgr.Instance?.RemovePlane(plane.Id);
                AuraDaoMgr.Instance?.UnregisterPlane(plane.Id);
                plane.MarkDestroyed();
            }
            _planes.Clear();
            MainPlane = null;
            if (ReferenceEquals(Instance, this)) Instance = null;
        }

    public IEnumerator Save()
        {
            yield break;
        }

    public IEnumerator Load()
        {
            yield break;
        }
    }
}
