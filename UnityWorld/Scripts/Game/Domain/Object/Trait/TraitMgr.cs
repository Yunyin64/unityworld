using System.Collections;
using UnityWorld.Core;
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
        /// <summary>当前特质管理器</summary>
        public static TraitMgr? Instance { get; private set; }

        /// <summary>管理器名称</summary>
        public string Name => "TraitMgr";

        /// <summary>管理器说明，当前为空</summary>
        public string Desc => "";

        // int → 该 NPC 当前持有的 Trait 实例表（TraitId → Trait）
        /// <summary>各角色持有的特质实例表</summary>
        private readonly Dictionary<int, Dictionary<TraitId, Trait>> _traitTable = new();

        // 特质定义数据源（由外部注入）
        /// <summary>提供特质配置的数据管理器</summary>
        private IDataMgrBase<TraitDefine>? _traitDefineMgr;

        /// <summary>登记当前特质管理器实例</summary>
        public TraitMgr()
        {
            Instance = this;
        }

        /// <summary>保留管理器初始化入口</summary>
        public void Init()
        {
             
        }

        /// <summary>注入特质定义数据源</summary>
        public void SetDefineMgr(IDataMgrBase<TraitDefine> traitDefineMgr) => _traitDefineMgr = traitDefineMgr;

        // ── 注册 ──────────────────────────────────────────

        /// <summary>为 NPC 初始化空的 Trait 槽（NPC 创建时调用）</summary>
        public void Register(int id)
            => _traitTable.TryAdd(id, new Dictionary<TraitId, Trait>());

        /// <summary>NPC 销毁时释放数据</summary>
        public void Unregister(int id)
            => _traitTable.Remove(id);

        // ── 添加 ──────────────────────────────────────────

        /// <summary>
        /// 给 NPC 添加一个 Trait 实例，属性修正应用仍待接入。
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
        public Trait? GetTrait(int id, TraitId traitId)
            => _traitTable.TryGetValue(id, out var dict) && dict.TryGetValue(traitId, out var t)
                ? t : null;

        /// <summary>获取 NPC 当前持有的所有 Trait 实例（只读快照）</summary>
        public IReadOnlyCollection<Trait> GetTraits(int id)
            => _traitTable.TryGetValue(id, out var dict)
                ? dict.Values
                : Array.Empty<Trait>();

        /// <summary>NPC 是否持有指定 Trait</summary>
        public bool HasTrait(int id, TraitId traitId)
            => _traitTable.TryGetValue(id, out var dict) && dict.ContainsKey(traitId);

        // ── 存档恢复 ──────────────────────────────────────

        /// <summary>
        /// 重新应用 NPC 所有 Trait 的属性修正（存档加载后调用）。
        /// 当前仅撤销旧修正，写入新修正待接入。
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

        /// <summary>检查特质定义是否有修正，实际应用逻辑预留</summary>
        private void ApplyModifiers(StatBlock stats, Trait trait)
        {
            if (_traitDefineMgr == null) return;
            var define = _traitDefineMgr.Get(trait.Id.Value);
            if (define == null || !define.HasModifiers) return;

        }

        /// <summary>按特质来源键移除相关属性修正</summary>
        private static void RevokeModifiers(StatBlock stats, Trait trait)
            => stats.RemoveModifiersBySource($"trait:{trait.Id.Value}");

        

        /// <summary>保留时步回调</summary>
        public void Tick(float deltaTime)
        {
            
            
        }


        /// <summary>保留世界启动回调</summary>
        public void Begin()
        {
             
        }

        /// <summary>保留帧更新回调</summary>
        public void Update()
        {
             
        }

        /// <summary>保留渲染回调</summary>
        public void Render(float dt)
        {
             
        }

        /// <summary>保留世界结束回调</summary>
        public void End()
        {
             
        }

    /// <summary>结束存档流程，当前无写入</summary>
    public IEnumerator Save()
        {
            yield break;
        }

    /// <summary>结束读档流程，当前无读取</summary>
    public IEnumerator Load()
        {
            yield break;
        }
    }
}
