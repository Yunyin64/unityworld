using System.Collections.Generic;
using UnityWorld.Game.Data;
using UnityWorld.Game.World;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC位置系统：管理 NPC 在哪个位面、哪个地块上
    /// </summary>
    public class NpcSystemPosition : NpcSystemBase<NpcPositionData>
    {
        protected override Dictionary<int, NpcPositionData> _dataTable { get ; set  ; } = new();

        /// <summary>
        /// NPC 诞生时：创建位置数据（默认值）并注册
        /// </summary>
        public override void OnEntityBorn(BirthContext context)
        {
            var npc = context.MainNpc;
            Register(npc, new NpcPositionData());
        }

        public override void OnTick(Npc npc, float deltaTime)
        {
            
        }

        public NpcPositionData GetPosition(int id)
            => _dataTable.TryGetValue(id, out var data) && data.IsPlaced ? data.Clone() : null;

        /// <summary>位置和位面成员只在全部检查通过后一起写入。</summary>
        public bool TryPlace(int id, int planeId, TileId tile)
        {
            WorldMgr.EnsureMutable();
            if (NpcMgr.Instance.BehaviorSystem.GetPrimary(id) is MoveBehavior { IsWeeklyManaged: true }) return false;
            return PlaceCore(id, planeId, tile);
        }

        private bool PlaceCore(int id, int planeId, TileId tile)
        {
            var plane = PlaneMgr.Instance?.GetPlaneById(planeId);
            if (NpcMgr.Instance.GetById(id) == null || !_dataTable.TryGetValue(id, out var data) ||
                plane == null || !plane.TileSystem.CanWalk(tile)) return false;
            bool entering = !data.IsPlaced || data.PlaneId != planeId;
            long next = entering ? checked(plane.NpcData.NextBirthOrder + 1) : plane.NpcData.NextBirthOrder;
            foreach (var old in PlaneMgr.Instance.GetAll())
                if (old.Id.Value != planeId) old.NpcSystem.RemoveMember(id);
            plane.NpcSystem.AddMember(id);
            if (entering) data.BirthOrder = plane.NpcData.NextBirthOrder;
            plane.NpcData.NextBirthOrder = next;
            data.PlaneId = planeId; data.TileId = tile; data.IsPlaced = true;
            return true;
        }

        /// <summary>行为到达时也由本系统写位置，不能让位面或行为各写一份坐标。</summary>
        internal bool Arrive(int id, int planeId, TileId from, TileId target)
        {
            var data = GetPosition(id);
            if (data == null || data.PlaneId != planeId || data.TileId != from || from.DistanceTo(target) != 1)
                return false;
            return PlaceCore(id, planeId, target);
        }

        internal void SetMoving(int id, bool moving)
        {
            if (_dataTable.TryGetValue(id, out var data))
                data.MoveState = moving ? NpcTypes.MoveState.Walking : NpcTypes.MoveState.Idle;
        }

        /// <summary>位面销毁只解除落地，不销毁NPC实体；以后仍可通过本系统重新放置。</summary>
        internal void LeavePlane(int id, int planeId)
        {
            if (_dataTable.TryGetValue(id, out var data) && data.IsPlaced && data.PlaneId == planeId)
            {
                NpcMgr.Instance.BehaviorSystem.StopWeeklyMove(id);
                data.IsPlaced = false; data.BirthOrder = -1; data.MoveState = NpcTypes.MoveState.Idle;
            }
        }

        public IReadOnlyList<TileId> GetMoveCandidates(int id)
        {
            var mgr = NpcMgr.Instance;
            var npc = mgr.GetById(id) ?? throw new ArgumentException("NPC不存在。");
            var data = GetPosition(id) ?? throw new InvalidOperationException("NPC尚未落地。");
            if (!mgr.BioSystem.GetData(id).IsAlive || mgr.BehaviorSystem.GetPrimary(id) != null)
                return Array.Empty<TileId>();
            var plane = PlaneMgr.Instance?.GetPlaneById(data.PlaneId);
            if (plane == null || !plane.TileSystem.CanWalk(data.TileId)) return Array.Empty<TileId>();
            var result = new List<TileId> { data.TileId };
            if (GetActionSpeed(npc) > 0)
                for (int direction = 0; direction < 6; direction++)
                {
                    var target = data.TileId.Neighbor(direction);
                    if (plane.TileSystem.CanWalk(target)) result.Add(target);
                }
            return result.AsReadOnly();
        }

        /// <summary>位置提供中心和半径，实际空间查询交给本位面的地块系统。</summary>
        public IReadOnlyList<Tile> GetVisibleTiles(int id)
        {
            var npc = NpcMgr.Instance.GetById(id) ?? throw new ArgumentException("NPC不存在。");
            var data = GetPosition(id) ?? throw new InvalidOperationException("NPC尚未落地。");
            var plane = PlaneMgr.Instance?.GetPlaneById(data.PlaneId);
            if (plane?.GetTile(data.TileId) == null) throw new InvalidOperationException("NPC所在位面或地块不存在。");
            return plane.TileSystem.GetTilesInRange(data.TileId, GetDisplayRadius(npc));
        }

        public float GetActionSpeed(Npc npc) => ReadExplorationValue(npc, "ExploreActionSpeed");
        public double GetDisplayRadius(Npc npc) => Math.Floor((double)ReadExplorationValue(npc, "ExploreDisplayRadius"));

        internal static void ValidateExplorationDefinitions()
        {
            ValidateDefinition(StatDefineMgr.Instance?.Get("ExploreActionSpeed"), "ExploreActionSpeed");
            ValidateDefinition(StatDefineMgr.Instance?.Get("ExploreDisplayRadius"), "ExploreDisplayRadius");
        }

        private static void ValidateDefinition(StatDefine define, string id)
        {
            if (define == null || define.ID != id || define.Type != "Npc" || define.BaseType != StatBaseType.Derived ||
                !float.IsFinite(define.DefaultValue) || define.DefaultValue < 0 || define.MinValue != 0 ||
                define.MaxValue is float max && (!float.IsFinite(max) || max < 0))
                throw new InvalidOperationException($"探索属性{id}缺失或定义非法。");
        }

        private static float ReadExplorationValue(Npc npc, string id)
        {
            ArgumentNullException.ThrowIfNull(npc);
            ValidateDefinition(npc.Stats?.FindDefinition(id), id);
            // 直接复用现有属性计算；不新建属性包装类，也不以默认0掩盖缺定义。
            float value = npc.Stats.GetReadOnly(id);
            if (!float.IsFinite(value) || value < 0) throw new InvalidOperationException($"探索属性{id}的最终值非法。");
            return value;
        }

        public override void Unregister(int id)
        {
            foreach (var plane in PlaneMgr.Instance?.GetAll() ?? Enumerable.Empty<Plane>()) plane.NpcSystem.RemoveMember(id);
            base.Unregister(id);
        }

        public void Clear()
        {
            foreach (int id in _dataTable.Keys.ToArray()) Unregister(id);
        }
    }
}
