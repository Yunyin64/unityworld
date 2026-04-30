using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 关系数据：存储 NPC 与其他 NPC 之间的关系
    /// </summary>
    public class NpcRelationshipData : IDomainDataBase
    {
        // ── 关系列表 ────────────────────────────────────

        /// <summary>关系表：目标 int → 关系值（正=友好，负=敌对）</summary>
        public Dictionary<int, float> Relations { get; set; } = new();

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
