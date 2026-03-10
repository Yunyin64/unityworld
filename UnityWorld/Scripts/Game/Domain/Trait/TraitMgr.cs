using UnityWorld.Game.Data;
namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Trait 运行时管理器：管理所有 NPC 的 Trait 实例集合。
    /// <para>
    /// 设计约定：<br/>
    /// - 静态定义数据由 <see cref="TraitDefineMgr"/> 负责，本类只管运行时实例。<br/>
    /// - 同一 TraitId 在一个 NPC 上最多存在一个实例，层数用 <see cref="Trait.Stacks"/> 表示。<br/>
    /// - AddTrait / RemoveTrait 时立即写入 / 撤销 StatModifier。
    /// </para>
    /// </summary>
    public class TraitMgr:IDomainMgrBase
    {
        public static TraitMgr? Instance { get; private set; }

        // NpcId → 该 NPC 当前持有的 Trait 实例表（TraitId → Trait）
        private readonly Dictionary<NpcId, Dictionary<TraitId, Trait>> _traitTable = new();

        // 特质定义数据源（由外部注入）
        private IDataMgrBase<TraitDefine>? _traitDefineMgr;

        public TraitMgr()
        {
            Instance = this;
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        /// <summary>注入特质定义数据源</summary>
        public void SetDefineMgr(IDataMgrBase<TraitDefine> traitDefineMgr) => _traitDefineMgr = traitDefineMgr;

        // ── 注册 ──────────────────────────────────────────

        /// <summary>为 NPC 初始化空的 Trait 槽（NPC 创建时调用）</summary>
        public void Register(NpcId id)
            => _traitTable.TryAdd(id, new Dictionary<TraitId, Trait>());

        /// <summary>NPC 销毁时释放数据</summary>
        public void Unregister(NpcId id)
            => _traitTable.Remove(id);

        // ── 添加 ──────────────────────────────────────────

        /// <summary>
        /// 给 NPC 添加一个 Trait 实例，并立即将其 StatModifier 写入 StatBlock。
        /// 若已持有同 ID 的 Trait，则忽略（层数由外部调用 Trait.AddStack 管理）。
        /// </summary>
        /// <returns>true=添加成功，false=已存在或 NPC 未注册</returns>
        public bool AddTrait(Npc npc, TraitId traitId)
        {
            if (!_traitTable.TryGetValue(npc.Id, out var dict)) return false;
            if (dict.ContainsKey(traitId)) return false;

            var trait = new Trait(traitId, npc.Id);
            dict[traitId] = trait;
            ApplyModifiers(npc.Stats, trait);
            return true;
        }

        // ── 移除 ──────────────────────────────────────────

        /// <summary>
        /// 移除 NPC 的某个 Trait，并立即撤销其 StatModifier。
        /// </summary>
        /// <returns>true=移除成功，false=不存在</returns>
        public bool RemoveTrait(Npc npc, TraitId traitId)
        {
            if (!_traitTable.TryGetValue(npc.Id, out var dict)) return false;
            if (!dict.Remove(traitId, out var trait)) return false;

            RevokeModifiers(npc.Stats, trait);
            return true;
        }

        // ── 查询 ──────────────────────────────────────────

        /// <summary>获取 NPC 持有的某个 Trait 实例（不存在返回 null）</summary>
        public Trait? GetTrait(NpcId id, TraitId traitId)
            => _traitTable.TryGetValue(id, out var dict) && dict.TryGetValue(traitId, out var t)
                ? t : null;

        /// <summary>获取 NPC 当前持有的所有 Trait 实例（只读快照）</summary>
        public IReadOnlyCollection<Trait> GetTraits(NpcId id)
            => _traitTable.TryGetValue(id, out var dict)
                ? dict.Values
                : Array.Empty<Trait>();

        /// <summary>NPC 是否持有指定 Trait</summary>
        public bool HasTrait(NpcId id, TraitId traitId)
            => _traitTable.TryGetValue(id, out var dict) && dict.ContainsKey(traitId);

        // ── 存档恢复 ──────────────────────────────────────

        /// <summary>
        /// 重新应用 NPC 所有 Trait 的属性修正（存档加载后调用）。
        /// 先清除所有旧 Modifier，再按最新 TraitDefine 重新写入。
        /// </summary>
        public void ReapplyAll(Npc npc)
        {
            if (!_traitTable.TryGetValue(npc.Id, out var dict)) return;

            foreach (var trait in dict.Values)
                RevokeModifiers(npc.Stats, trait);

            foreach (var trait in dict.Values)
                ApplyModifiers(npc.Stats, trait);
        }

        // ── Tick ──────────────────────────────────────────

        /// <summary>驱动所有 Trait 计时（每帧调用）</summary>
        public void OnTick(Npc npc, float deltaTime)
        {
            if (!_traitTable.TryGetValue(npc.Id, out var dict)) return;

            foreach (var trait in dict.Values)
                trait.TickTime(deltaTime);
        }

        // ── 内部工具 ──────────────────────────────────────

        private void ApplyModifiers(StatBlock stats, Trait trait)
        {
            if (_traitDefineMgr == null) return;
            var define = _traitDefineMgr.Get(trait.Id.Value);
            if (define == null || !define.HasModifiers) return;

            foreach (var (statId, modifier) in define.BuildModifiers())
                stats.AddModifier(statId, modifier);
        }

        private static void RevokeModifiers(StatBlock stats, Trait trait)
            => stats.RemoveModifiersBySource($"trait:{trait.Id.Value}");

        

        public void Tick(float deltaTime)
        {
            
            
        }
    }
}
