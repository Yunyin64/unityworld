using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 属性数据：HP、MP、等级等运行时战斗属性
    /// </summary>
    public class NpcStatsData : IDomainDataBase
    {
        // ── 基础属性 ────────────────────────────────────

        /// <summary>当前等级</summary>
        public int Level { get; set; } = 1;

        /// <summary>当前生命值</summary>
        public float Hp { get; set; } = 100f;

        /// <summary>当前法力值</summary>
        public float Mp { get; set; } = 50f;

        // ── 日志 ────────────────────────────────────

        public void Log()
        {
            // TODO: 由 DomainData Log 技能补全
        }
    }
}
