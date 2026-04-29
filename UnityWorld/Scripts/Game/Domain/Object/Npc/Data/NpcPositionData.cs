using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC位置数据：基于位面 + 六边形地块坐标
    /// </summary>
    public class NpcPositionData : IDomainDataBase
    {
        /// <summary>所在位面ID</summary>
        public int PlaneId { get; set; }

        /// <summary>所在地块坐标（Axial 六边形）</summary>
        public TileId TileId { get; set; }

        /// <summary>当前运动状态</summary>
        public NpcTypes.MoveState MoveState { get; set; } = NpcTypes.MoveState.Idle;

        // ── 日志 ────────────────────────────────────

        public void Log()
        {
            // TODO: 由 DomainData Log 技能补全
        }
    }
}
