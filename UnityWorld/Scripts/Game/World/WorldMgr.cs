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
        /// <summary>
        /// 初始化世界（游戏启动时调用）：
        ///   1. 加载外部数据定义（GameDataMgr）
        ///   2. 初始化世界时间
        ///   3. 初始化NPC管理器
        ///   4. 初始化位面管理器（含主世界）
        /// </summary>
        public static void Initialize(int seed = 12345)
        {
            GameDataMgr.Initialize();
            WorldTime.Initialize(seed);
            _mgrs.Add(new NpcMgr(seed));
            _mgrs.Add(new PlaneMgr());
            
            foreach (var mgr in _mgrs) mgr.Initialize();  // 初始化时先Tick一次，建立初始状态（如生成NPC）
        }

        public static void Start()
        {
            
        Console.WriteLine("=== 世界初始化完成 ===\n");
        }

        /// <summary>
        /// 全局Tick入口：按优先级顺序驱动所有子系统
        /// 顺序：WorldTime → PlaneMgr → NpcMgr → (后续系统...)
        /// </summary>
        public static void Tick(float deltaTime)
        {
            // ① 推进世界物理时间
            WorldTime.Advance(deltaTime);

            foreach (var mgr in _mgrs) mgr.Tick(deltaTime);  // 统一驱动所有注册的子系统（如 TraitMgr）
        }
    }
}