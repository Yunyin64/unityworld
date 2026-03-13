using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.World
{
    /// <summary>
    /// World管理器：统一Tick入口，持有所有顶层子系统
    /// </summary>
    public static class WorldMgr
    {
        public static List<IDomainMgrBase> _mgrs = new();  // 统一管理列表，方便统一Tick

        // ── 元气系统 ──────────────────────────────────────

        /// <summary>元气天道管理器：持有原初快照、计算五行收支偏差、暴露失衡权重</summary>
        public static AuraDaoMgr AuraDaoMgr { get; private set; } = new();

        /// <summary>地块元气系统：每 Tick 将 TileModifier 累积到 Tile.CurrentAura 并清理过期修正</summary>
        public static TileSystemAura TileSystemAura { get; private set; } = new();

        /// <summary>
        /// 初始化世界（游戏启动时调用）：
        ///   1. 加载外部数据定义（GameDataMgr）
        ///   2. 初始化世界时间
        ///   3. 初始化NPC管理器
        ///   4. 初始化位面管理器（含主世界，并拍摄原初元气快照）
        /// </summary>
        public static void Initialize(int seed = 12345)
        {
            GameDataMgr.Initialize();
            WorldTime.Initialize(seed);
            _mgrs.Add(new NpcMgr(seed));

            // PlaneMgr 需要 AuraDaoMgr 引用，以便 Fill() 完成后自动拍摄快照
            var planeMgr = new PlaneMgr { AuraDaoMgr = AuraDaoMgr };
            _mgrs.Add(planeMgr);
            
            foreach (var mgr in _mgrs) mgr.Init();  // 初始化时先Tick一次，建立初始状态（如生成NPC）
        }

        public static void Start()
        {
            var eventCount = EventDefineMgr.Instance?.GetAll() is var all ? System.Linq.Enumerable.Count(all) : 0;
            LogMgr.Dbg("[WorldMgr] 已加载 {0} 个事件定义", eventCount);
            Console.WriteLine("=== 世界初始化完成 ===\n");
        }

        /// <summary>
        /// 全局Tick入口：按优先级顺序驱动所有子系统
        /// 顺序：WorldTime → PlaneMgr → NpcMgr → TileSystemAura → AuraDaoMgr → (后续系统...)
        /// </summary>
        public static void Tick(float deltaTime)
        {
            // ① 推进世界物理时间
            WorldTime.Advance(deltaTime);

            foreach (var mgr in _mgrs) mgr.Tick(deltaTime);  // 统一驱动所有注册的子系统（如 TraitMgr）

            // ② 元气系统：先累积 Modifier 到 CurrentAura，再让天道感知最新状态
            var planeMgr = PlaneMgr.Instance;
            if (planeMgr?.MainPlane != null)
            {
                TileSystemAura.Tick(planeMgr.MainPlane, deltaTime);
            }
            AuraDaoMgr.OnTick(deltaTime);
        }
    }
}