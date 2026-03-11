
namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 位面管理器：负责位面的创建、销毁、查询，以及驱动各位面Tick
    /// </summary>
    public class PlaneMgr:IDomainMgrBase
    {
        // ── 数据 ─────────────────────────────────────────

        private readonly Dictionary<PlaneId, Plane> _planes = new();

        public static PlaneMgr? Instance { get; private set; } 

        /// <summary>主世界（唯一，游戏初始化时创建）</summary>
        public Plane? MainPlane { get; private set; }

        // ── 初始化 ────────────────────────────────────────

        /// <summary>
        /// 初始化：创建主世界。
        /// </summary>
        /// <param name="mainPlaneName">主世界名称，默认"主世界"</param>
        public void Initialize()
        {
            Instance  = this;
            MainPlane = CreatePlane(PlaneConfig.ForMainPlane("主世界"));
            
            Console.WriteLine("主世界大小：{0}×{1}", MainPlane.Width, MainPlane.Height);
        }

        // ── 创建 ─────────────────────────────────────────

        /// <summary>
        /// 通用位面创建入口：接受完整的 <see cref="PlaneConfig"/> 参数，
        /// 生成唯一ID后构造并注册位面。
        /// </summary>
        /// <param name="config">位面配置，含名称、类型、时间流速、元气加成、初始地块等</param>
        /// <returns>新建的位面实例</returns>
        public Plane CreatePlane(PlaneConfig config)
        {
            var id    = PlaneIdGenerator.Generate();
            if (config.Kind == PlaneTypes.PlaneKind.MainPlane)
            {
                id = 0;
            }
            var plane = new Plane(id, config);
            _planes[id] = plane;
            PlaneGenerator.Fill(plane);   // 根据 Width × Height 填充初始地块
            return plane;
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
        /// 注意：位面内的NPC不会自动销毁，调用方应先处理NPC的迁移或清除。
        /// </summary>
        public bool Destroy(PlaneId id)
        {
            if (!_planes.TryGetValue(id, out var plane)) return false;
            if (plane.Kind == PlaneTypes.PlaneKind.MainPlane) return false; // 主世界不可销毁

            plane.MarkDestroyed();
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

        // ── NPC归属 ───────────────────────────────────────

        /// <summary>
        /// 查找某NPC当前所在的位面（线性查找，仅适合低频调用）
        /// </summary>
        public Plane? FindPlaneOfNpc(NpcId npcId)
        {
            foreach (var plane in _planes.Values)
                if (plane.ContainsNpc(npcId)) return plane;
            return null;
        }

        /// <summary>
        /// 将NPC从旧位面迁移到新位面（原子操作）。
        /// 同时更新 NpcSystemPosition 里的位置记录（可选传入）。
        /// </summary>
        public bool MoveNpc(NpcId npcId, PlaneId targetPlaneId,
                            TileId targetTileId = default,
                            NpcSystemPosition? posSystem = null)
        {
            if (!_planes.TryGetValue(targetPlaneId, out var target)) return false;
            if (target.State == PlaneTypes.PlaneState.Destroyed)    return false;

            // 从当前位面移除
            var current = FindPlaneOfNpc(npcId);
            current?.RemoveNpc(npcId);

            // 加入目标位面
            target.AddNpc(npcId);

            // 同步位置系统（如果传入）
            posSystem?.Teleport(npcId, targetPlaneId, targetTileId);

            return true;
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
    }
}