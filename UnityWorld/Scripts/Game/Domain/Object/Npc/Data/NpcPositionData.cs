using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC位置数据：基于位面 + 六边形地块坐标
    /// </summary>
    public class NpcPositionData : IDomainDataBase
    {
        /// <summary>默认零坐标不是出生；只有受校验的放置成功后才为 true。</summary>
        public bool IsPlaced { get; internal set; }
        /// <summary>成功进入本位面时的稳定序号；只是位置数据，不是额外的成员登记。</summary>
        public long BirthOrder { get; internal set; } = -1;
        /// <summary>所在位面ID</summary>
        public int PlaneId { get; set; }

        /// <summary>所在地块坐标（Axial 六边形）</summary>
        public TileId TileId { get; set; }

        /// <summary>当前运动状态</summary>
        public NpcTypes.MoveState MoveState { get; set; } = NpcTypes.MoveState.Idle;

        /// <summary>浅复制角色当前位面和地块位置数据</summary>
        public NpcPositionData Clone()
        {
            var copy = (NpcPositionData)MemberwiseClone();
            return copy;
        }
        /// <summary>通过领域数据接口复制位置数据</summary>
        IDomainDataBase IDomainDataBase.Clone() => Clone();


        // ── 日志 ────────────────────────────────────

        /// <summary>保留角色位置日志入口</summary>
        public void Log()
        {
            // TODO: 由 DomainData Log 技能补全
        }
    }
}
