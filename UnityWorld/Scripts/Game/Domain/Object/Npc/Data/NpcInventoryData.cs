using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 物品栏数据：管理 NPC 持有的物品集合
    /// </summary>
    public class NpcInventoryData : IDomainDataBase
    {
        // ── 物品列表 ────────────────────────────────────

        /// <summary>持有的物品 ID 列表</summary>
        public List<string> ItemIds { get; set; } = new();

        // ── 日志 ────────────────────────────────────

        public void Log()
        {
            // TODO: 由 DomainData Log 技能补全
        }
    }
}
