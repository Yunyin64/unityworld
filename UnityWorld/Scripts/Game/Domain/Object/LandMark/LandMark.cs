namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 地标运行时实体。
    /// 代表世界中已实际生成的一个地标（自然奇观或后天建筑）。
    /// 由 <see cref="LandMarkMgr"/> 统一管理生命周期。
    /// </summary>
    public class LandMark
    {
        /// <summary>运行时唯一 ID（8位随机整数）</summary>
        public int Id { get; }

        /// <summary>对应的 Define ID（策划配置的地标类型）</summary>
        public string DefineId { get; }

        /// <summary>所在地块坐标（Axial）</summary>
        public TileId Position { get; }

        /// <summary>所在位面 ID</summary>
        public int PlaneId { get; }

        /// <summary>是否有效（销毁后置 false）</summary>
        public bool IsActive { get; set; } = true;

        public LandMark(int id, string defineId, TileId position, int planeId)
        {
            Id       = id;
            DefineId = defineId;
            Position = position;
            PlaneId  = planeId;
        }

        public override string ToString() => $"LandMark({Id}, {DefineId}, {Position})";
    }
}
