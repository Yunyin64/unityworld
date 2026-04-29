using UnityWorld.Game.Data;
using UnityWorld.Core;
namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 特质系统：管理 NPC 的 Trait 实例列表，并将属性修正同步到 StatBlock。
    /// <para>
    /// 设计约定：<br/>
    /// - 同一 TraitId 在一个 NPC 上最多存在一个实例（不重复叠加，层数用 Trait.Stacks 表示）。<br/>
    /// - AddTrait / RemoveTrait 时立即写入 / 撤销 StatModifier。<br/>
    /// - 加载存档后调用 <see cref="ReapplyAll"/> 重建所有修正，防止 TraitDefine 更新后失效。<br/>
    /// - TraitMgr 由外部注入，系统本身不持有全局引用。
    /// </para>
    /// </summary>
    public class NpcSystemTrait : NpcSystemBase<NpcTraitData>
    {
        protected override Dictionary<int, NpcTraitData> _dataTable { get; set; } = new();

        // ── 注册 ──────────────────────────────────────────

        /// <summary>为 NPC 初始化空的 Trait 槽（NPC 创建时调用）</summary>
        public void Register(int id)
            => _dataTable.TryAdd(id, new NpcTraitData());

        /// <summary>NPC 销毁时释放数据</summary>
        public void Unregister(int id)
            => _dataTable.Remove(id);

        // ── 添加 ──────────────────────────────────────────

        /// <summary>
        /// 给 NPC 添加一个 Trait 实例，并立即将其 StatModifier 写入 StatBlock。
        /// 若已持有同 ID 的 Trait，则忽略（层数由外部调用 Trait.AddStack 管理）。
        /// </summary>
        /// <returns>true=添加成功，false=已存在或定义缺失</returns>
        public bool AddTrait(Npc npc, TraitId traitId)
        {
            return true;
        }

        // ── 移除 ──────────────────────────────────────────

        /// <summary>
        /// 移除 NPC 的某个 Trait，并立即撤销其 StatModifier。
        /// </summary>
        /// <returns>true=移除成功，false=不存在</returns>
        public bool RemoveTrait(Npc npc, TraitId traitId)
        {
            return true;
        }

        // ── 查询 ──────────────────────────────────────────

        /// <summary>获取 NPC 持有的某个 Trait 实例（不存在返回 null）</summary>
        public Trait GetTrait(int id, TraitId traitId)
        {
            return null;
        }

        /// <summary>获取 NPC 当前持有的所有 Trait 实例（只读快照）</summary>
        public IReadOnlyCollection<Trait> GetTraits(int id)
        {
            return null;
        }

        /// <summary>NPC 是否持有指定 Trait</summary>
        public bool HasTrait(int id, TraitId traitId)
        {
            return true;
        }

        // ── 存档恢复 ──────────────────────────────────────

        /// <summary>
        /// 重新应用 NPC 所有 Trait 的属性修正（存档加载后调用）。
        /// 先清除所有来源为 "trait:*" 的旧 Modifier，再按最新 TraitDefine 重新写入。
        /// </summary>
        public void ReapplyAll(Npc npc)
        {
            
        }

        /// <summary>
        /// NPC 诞生时：创建空 Trait 数据并注册
        /// </summary>
        public override void OnEntityBorn(BirthContext context)
        {
            var npc = context.MainNpc;
            Register(npc, new NpcTraitData());
        }

        // ── Tick ──────────────────────────────────────────

        public override void OnTick(Npc npc, float deltaTime)
        {
            
        }

        // ── 内部工具 ──────────────────────────────────────

        private void ApplyModifiers(StatBlock stats, Trait trait)
        {
            
        }

        private static void RevokeModifiers(StatBlock stats, Trait trait)
            => stats.RemoveModifiersBySource($"trait:{trait.Id.Value}");
    }
}
