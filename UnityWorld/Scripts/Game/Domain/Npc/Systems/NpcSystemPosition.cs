using System.Collections.Generic;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC位置数据：基于位面 + 六边形地块坐标
    /// </summary>
    public class NpcPositionData
    {
        /// <summary>所在位面ID</summary>
        public PlaneId PlaneId { get; set; }

        /// <summary>所在地块坐标（Axial 六边形）</summary>
        public TileId TileId { get; set; }

        /// <summary>当前运动状态</summary>
        public NpcTypes.MoveState MoveState { get; set; } = NpcTypes.MoveState.Idle;
    }

    /// <summary>
    /// NPC位置系统：管理 NPC 在哪个位面、哪个地块上
    /// </summary>
    public class NpcSystemPosition : NpcSystemBase
    {
        private readonly Dictionary<NpcId, NpcPositionData> _posTable = new();

        // ── 注册 ─────────────────────────────────────────

        /// <summary>注册NPC初始位置（NPC创建时调用）</summary>
        public void Register(Npc npc,  int x, int y,int planeId = 0)
        {
            _posTable[npc.Id] = new NpcPositionData
            {
                PlaneId = planeId,
                TileId  = new TileId(x, y),
            };
        }

        // ── 查询 ─────────────────────────────────────────

        /// <summary>获取NPC当前位置，不存在返回 null</summary>
        public NpcPositionData? GetPosition(NpcId id)
            => _posTable.TryGetValue(id, out var data) ? data : null;

        /// <summary>获取NPC所在位面ID</summary>
        public PlaneId? GetPlaneId(NpcId id)
            => _posTable.TryGetValue(id, out var data) ? data.PlaneId : null;

            /// <summary>获取NPC所在位面实例</summary>
            /// <remarks>如果位面已销毁或未注册，返回 null</remarks>
        public Plane? GetPlane(NpcId id)
        {
            if (!_posTable.TryGetValue(id, out var data)) return null;
            return PlaneMgr.Instance?.GetPlaneById(data.PlaneId);
        }

        /// <summary>获取NPC所在地块坐标</summary>
        public TileId? GetTileId(NpcId id)
            => _posTable.TryGetValue(id, out var data) ? data.TileId : null;

        public Tile? GetTile(NpcId id)
        {
            if (!_posTable.TryGetValue(id, out var data)) return null;
            var plane = PlaneMgr.Instance?.GetPlaneById(data.PlaneId);
            return plane != null && plane.Tiles.TryGetValue(data.TileId, out var tile) ? tile : null;
        }
        
        // ── 移动 ─────────────────────────────────────────

        /// <summary>
        /// 将NPC瞬移到同位面的指定地块。
        /// 不校验地块是否可通行，由上层逻辑（AI/事件）负责校验。
        /// </summary>
        public bool MoveTo(NpcId id, TileId targetTile)
        {
            if (!_posTable.TryGetValue(id, out var data)) return false;
            data.TileId    = targetTile;
            data.MoveState = NpcTypes.MoveState.Walking;
            return true;
        }

        /// <summary>
        /// 将NPC跨位面传送（同时更新位面归属，需配合 PlaneMgr 使用）。
        /// </summary>
        public bool Teleport(NpcId id, PlaneId targetPlane, TileId targetTile)
        {
            if (!_posTable.TryGetValue(id, out var data)) return false;
            data.PlaneId   = targetPlane;
            data.TileId    = targetTile;
            data.MoveState = NpcTypes.MoveState.Idle;
            return true;
        }

        /// <summary>
        /// 计算从 <paramref name="from"/> 朝某方向走一步后的坐标（0~5，顺时针）。
        /// 方向定义与 <see cref="TileId.Directions"/> 一致。
        /// </summary>
        public static TileId StepInDirection(TileId from, int direction)
            => from.Neighbor(direction);

        /// <summary>
        /// 尝试让 NPC 朝指定方向走一步，并校验目标格是否在位面边界内。
        /// </summary>
        /// <param name="id">NPC ID</param>
        /// <param name="direction">方向 0~5（0=右，顺时针）</param>
        /// <param name="plane">NPC 当前所在位面（用于边界校验）</param>
        /// <returns>
        ///   <c>MoveResult.Success</c>：移动成功；<br/>
        ///   <c>MoveResult.OutOfBounds</c>：目标格超出位面边界；<br/>
        ///   <c>MoveResult.NotFound</c>：NPC 位置未注册。
        /// </returns>
        public NpcTypes.MoveResult TryMove(NpcId id, int direction)
        {
            if (!_posTable.TryGetValue(id, out var data)) return NpcTypes.MoveResult.NotFound;

            var target = StepInDirection(data.TileId, direction);
            var plane = PlaneMgr.Instance?.GetPlaneById(data.PlaneId);
            if (plane == null || !plane.IsInBounds(target)) return NpcTypes.MoveResult.OutOfBounds;

            data.TileId    = target;
            data.MoveState = NpcTypes.MoveState.Walking;
            return NpcTypes.MoveResult.Success;
        }

        // ── Tick ─────────────────────────────────────────

        public override void OnTick(Npc npc, float deltaTime)
        {
            // 移动逻辑由 AI / 寻路系统驱动，位置系统只负责存储状态
        }
    }
}