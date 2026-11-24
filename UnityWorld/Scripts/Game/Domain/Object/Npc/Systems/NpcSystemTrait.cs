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
        /// <summary>按角色编号保存的特质数据</summary>
        protected override Dictionary<int, NpcTraitData> _dataTable { get; set; } = new();

        // ── 注册 ──────────────────────────────────────────

        /// <summary>为 NPC 初始化空的 Trait 槽（NPC 创建时调用）</summary>
        public void Register(int id)
            => _dataTable.TryAdd(id, new NpcTraitData());

        /// <summary>NPC 销毁时释放数据</summary>
        public override void Unregister(int id)
            => _dataTable.Remove(id);

        // ── 添加 ──────────────────────────────────────────

        /// <summary>保留赋予特质的接口，当前直接返回成功</summary>
        /// <returns>true=添加成功，false=已存在或定义缺失</returns>
        public bool AddTrait(Npc npc, TraitId traitId)
        {
            return true;
        }

        // ── 移除 ──────────────────────────────────────────

        /// <summary>
        /// 移除 NPC 特质的预留入口，当前直接返回成功。
        /// </summary>
        /// <returns>当前固定返回 true</returns>
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

        /// <summary>保留特质持有查询，当前直接返回真</summary>
        public bool HasTrait(int id, TraitId traitId)
        {
            return true;
        }

        // ── 存档恢复 ──────────────────────────────────────

        /// <summary>保留重新应用角色全部特质修正的入口</summary>
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

        /// <summary>保留角色特质的时步入口</summary>
        public override void OnTick(Npc npc, float deltaTime)
        {
            
        }

        // ── 内部工具 ──────────────────────────────────────

        /// <summary>保留写入单个特质属性修正的入口</summary>
        private void ApplyModifiers(StatBlock stats, Trait trait)
        {
            
        }

        /// <summary>按特质来源键移除相关属性修正</summary>
        private static void RevokeModifiers(StatBlock stats, Trait trait)
            => stats.RemoveModifiersBySource($"trait:{trait.Id.Value}");
    }
}
