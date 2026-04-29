using System.Collections;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Flag 管理器：变量黑板系统总入口
    /// 维护各主体维度的 FlagBoard 和全局 GlobalFlagBoard
    /// 用于叙事状态追踪、事件进度记录、任意 KV 存储
    ///
    /// 扩展方式：新增主体类型时，仿照现有字段增加一个 FlagBoard&lt;TKey&gt; 属性即可
    /// </summary>
    public class FlagMgr : IDomainMgrBase
    {
        // ── 单例 ─────────────────────────────────────────────
        public static FlagMgr Instance { get; private set; }

        // ── IDomainMgrBase ────────────────────────────────────
        public string Name => "FlagMgr";
        public string Desc => "变量黑板系统：NPC/Tile/Plane/全局 KV 状态存储";

        // ── 各维度黑板 ────────────────────────────────────────

        /// <summary>NPC 维度黑板（int → KV）</summary>
        public FlagBoard<int> Npc { get; } = new();

        /// <summary>地块维度黑板（TileId → KV）</summary>
        public FlagBoard<TileId> Tile { get; } = new();

        /// <summary>位面维度黑板（PlaneId → KV）</summary>
        public FlagBoard<int> Plane { get; } = new();

        /// <summary>全局黑板（跨主体的叙事状态，如道统是否现世）</summary>
        public GlobalFlagBoard Global { get; } = new();

        // ── 构造 ──────────────────────────────────────────────
        public FlagMgr()
        {
            Instance = this;
        }

        // ── IDomainMgrBase 生命周期 ───────────────────────────

        /// <summary>无耦合初始化：清空所有黑板</summary>
        public void Init()
        {
            Npc.ClearAll();
            Tile.ClearAll();
            Plane.ClearAll();
            Global.ClearAll();
            LogMgr.Dbg("[FlagMgr] 初始化完成");
        }

        public void Begin()  { }
        public void Tick(float deltaTime) { }
        public void Update() { }
        public void Render(float dt) { }

        /// <summary>清理：释放所有 Flag 数据</summary>
        public void End()
        {
            Npc.ClearAll();
            Tile.ClearAll();
            Plane.ClearAll();
            Global.ClearAll();
        }

        public IEnumerator Save() { yield break; }
        public IEnumerator Load() { yield break; }

        // ── 便捷静态访问 ──────────────────────────────────────

        /// <summary>设置 NPC Flag（空值安全）</summary>
        public static void SetNpc(int id, string key, object value)
            => Instance?.Npc.Set(id, key, value);

        /// <summary>获取 NPC Flag（空值安全，不存在返回 null）</summary>
        public static object? GetNpc(int id, string key)
            => Instance?.Npc.Get(id, key);

        /// <summary>设置全局 Flag（空值安全）</summary>
        public static void SetGlobal(string key, object value)
            => Instance?.Global.Set(key, value);

        /// <summary>获取全局 Flag（空值安全，不存在返回 null）</summary>
        public static object? GetGlobal(string key)
            => Instance?.Global.Get(key);

        /// <summary>检查全局 Flag 是否为 true</summary>
        public static bool IsGlobal(string key)
            => Instance?.Global.GetBool(key) ?? false;

        public void Log()
        {
            
        }
    }
}
