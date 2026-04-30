using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 特质数据：存储 NPC 当前持有的 Trait 实例集合
    /// </summary>
    public class NpcTraitData : IDomainDataBase
    {
        // ── 特质表 ────────────────────────────────────

        /// <summary>该 NPC 当前持有的 Trait 实例表（TraitId → Trait）</summary>
        public Dictionary<TraitId, Trait> Traits { get; set; } = new();

        public IDomainDataBase Clone()
        {
            throw new NotImplementedException();
        }

        // ── 日志 ────────────────────────────────────

        public void Log()
        {
            // TODO: 由 DomainData Log 技能补全
        }
    }
}
