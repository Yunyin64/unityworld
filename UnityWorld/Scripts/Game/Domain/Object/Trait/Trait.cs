namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Trait（特质）运行时实例。
    /// <para>
    /// 设计约定：<br/>
    /// - 一个 NPC 同一个 TraitId 最多持有一个实例（不重复叠加）。<br/>
    /// - 叠加效果通过 <see cref="Stacks"/> 表示，由 TraitMgr/TraitSystem 管理。<br/>
    /// - 真实定义数据（名称、修正值等）存放在 TraitDefine，通过 TraitId 查询。
    /// </para>
    /// </summary>
    public class Trait
    {
        // ── 身份 ─────────────────────────────────────────

        /// <summary>对应的 TraitDefine.Id</summary>
        public TraitId Id { get; }

        /// <summary>持有此 Trait 的 NPC</summary>
        public NpcId OwnerId { get; }

        // ── 运行时状态 ────────────────────────────────────

        /// <summary>
        /// 当前层数（叠加次数）。最大层数由 TraitDefine 约束。
        /// 初始为 1，移除时递减到 0 才真正销毁。
        /// </summary>
        public int Stacks { get; private set; } = 1;

        /// <summary>
        /// 累计持续时间（秒）。
        /// 由 TraitSystemTimer 在 Tick 中更新；永久 Trait 可忽略此字段。
        /// </summary>
        public float ElapsedTime { get; private set; } = 0f;

        // ── 构造 ─────────────────────────────────────────

        public Trait(TraitId id, NpcId ownerId)
        {
            Id = id;
            OwnerId = ownerId;
        }

        // ── 层数操作 ──────────────────────────────────────

        /// <summary>增加层数，返回当前层数</summary>
        public int AddStack(int count = 1)
        {
            Stacks += count;
            return Stacks;
        }

        /// <summary>减少层数，返回当前层数（不会低于 0）</summary>
        public int RemoveStack(int count = 1)
        {
            Stacks = Math.Max(0, Stacks - count);
            return Stacks;
        }

        // ── 计时器 ────────────────────────────────────────

        /// <summary>由 TraitSystemTimer 每帧调用，累计经过时间</summary>
        internal void TickTime(float deltaTime) => ElapsedTime += deltaTime;

        // ── 到期 ──────────────────────────────────────────

        /// <summary>是否已到期，由 TraitSystemTimer 标记，TraitSystemEffect 负责清除</summary>
        public bool IsExpired { get; private set; } = false;

        /// <summary>标记为已到期（由 TraitSystemTimer 调用）</summary>
        internal void MarkExpired() => IsExpired = true;

        // ── 工具 ─────────────────────────────────────────

        public override string ToString() => $"Trait({Id.Value} x{Stacks} owner={OwnerId})";
    }
}
