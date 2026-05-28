using System.Collections;

namespace UnityWorld.Core
{
    /// <summary>
    /// 属性管理器：运行时 StatBlock 集中管理入口
    /// 按实体类型分 Dict 存储 StatBlock，支持统一生命周期管理
    /// </summary>
    public class StatMgr : IDomainMgrBase
    {
        // ── 单例 ─────────────────────────────────────────────
        public static StatMgr Instance { get; private set; }

        // ── IDomainMgrBase ────────────────────────────────────
        public string Name => "StatMgr";
        public string Desc => "属性系统：集中管理所有实体的 StatBlock";

        // ── 各类型 StatBlock 存储 ─────────────────────────────
        
        private readonly  Dictionary<string,Dictionary<int, StatBlock>> _Blocks = new();

        // ── 构造 ──────────────────────────────────────────────
        public StatMgr(int seed = 12345)
        {
            Instance = this;
        }

        // ── IDomainMgrBase 生命周期 ───────────────────────────

        /// <summary>无耦合初始化：清空所有 StatBlock</summary>
        public void Init()
        {
            _Blocks.Clear();
            LogMgr.Instance.Dbg("[StatMgr] 初始化完成");
        }

        public void Begin() { }
        public void Tick(float deltaTime) { }
        public void Update() { }
        public void Render(float dt) { }

        /// <summary>清理：释放所有 StatBlock</summary>
        public void End()
        {
            _Blocks.Clear();
            Instance = null;
        }

        public IEnumerator Save() { yield break; }
        public IEnumerator Load() { yield break; }

        // ── 创建 StatBlock ───────────────────────────────────

        /// <summary>
        /// 创建指定类型和 ID 的 StatBlock（惰性创建，不预填充 Entry）
        /// </summary>
        /// <param name="id">实体 ID</param>
        /// <param name="objectType">Object 类型（"Npc" / "Tile" / "Plane"）</param>
        /// <returns>创建的 StatBlock 引用</returns>
        public StatBlock CreateBlock(int id, Type objectType)
        {
            var block = new StatBlock();
            if(!_Blocks.ContainsKey(objectType.Name)) _Blocks[objectType.Name] = new();
            _Blocks[objectType.Name][id] = block;
            block.InitType(objectType.Name);
            return block;
        }


        // ── 获取 StatBlock ───────────────────────────────────

        /// <summary>获取 NPC 的 StatBlock</summary>
        public StatBlock GetBlock(object obj,int id)
            => _Blocks.TryGetValue(obj.GetType().Name, out var block) && block.TryGetValue(id, out var statBlock) ? statBlock : null;

        // ── 移除 StatBlock ───────────────────────────────────

        /// <summary>移除指定类型和 ID 的 StatBlock</summary>
        public void RemoveBlock(int id, Type objectType)
        {
            _Blocks[objectType.Name].Remove(id);
        }


        // ── 事件广播接口（预留）──────────────────────────────

        // TODO: Stat 变化事件广播
        // public event Action<int, string, float, float>? OnStatChanged;
        // 参数：ownerId, statId, oldValue, newValue

        /// <summary>日志输出（输出Name、Desc、存的数据信息的数量与概括）</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("[{0}] {1} | Blocks={2}",
                Name, Desc, _Blocks.Count);
        }
    }
}
