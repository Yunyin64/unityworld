using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Core;

namespace UnityWorld.Game.World
{
    /// <summary>
    /// World管理器：统一Tick入口，持有所有顶层子系统
    /// </summary>
    public static class WorldMgr
    {
        public static List<IDomainMgrBase> _domains = new();  // 统一管理列表，方便统一Tick
        public static List<IGameplayMgrBase> _gameplays = new();  // 统一gameplay列表，方便统一Tick

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
        ///   4. 初始化位面管理器
        /// </summary>
        public static void Initialize(int seed = 12345)
        {
            GameDataMgr.Initialize();
            WorldTime.Initialize(seed);
            _gameplays.Add(new GlyphMgr(seed));
            _gameplays.Add(new AuraDaoMgr(seed));
            _gameplays.Add(new CombatMgr(seed));
            _gameplays.Add(new CultivationMgr(seed));
            
            _domains.Add(new LuaMgr());
            _domains.Add(new APIMgr());
            
            
            _domains.Add(new StatMgr(seed));
            _domains.Add(new NpcMgr(seed));
            _domains.Add(new PlaneMgr());

            // ── 叙事系统 ──────────────────────────────────────
            _domains.Add(new StoryMgr(seed));
            _domains.Add(new BehaviorCardMgr());
            _domains.Add(new CardMgr(seed));
            _domains.Add(new GongFaMgr(seed));
            _domains.Add(new EquipMgr());

            foreach (var mgr in _domains) mgr.Init();  // 无耦合的初始化
            foreach (var mgr in _gameplays) mgr.Init();
        }

        public static void Start()
        {
            foreach (var mgr in _domains) mgr.Begin();  // 有耦合的初始化
            foreach (var mgr in _gameplays) mgr.Begin();
            
            LogMgr.Dbg("=== 世界初始化完成 ===\n");
        }

        /// <summary>
        /// 全局Tick入口：按优先级顺序驱动所有子系统
        /// 顺序：WorldTime → PlaneMgr → NpcMgr → TileSystemAura → AuraDaoMgr → (后续系统...)
        /// </summary>
        public static void Tick(float deltaTime)
        {
            // ① 推进世界物理时间
            WorldTime.Advance(deltaTime);

            foreach (var mgr in _domains) mgr.Tick(deltaTime);  // 统一驱动所有注册的子系统
            foreach (var mgr in _gameplays) mgr.Tick(deltaTime);

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