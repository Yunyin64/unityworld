using System.Collections.Generic;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 移动行为：NPC 正在移动中
    /// IsPrimary=true, CanMove=false（移动时不允许再做其他移动）
    /// </summary>
    public class MoveBehavior : BehaviorBase
    {
        /// <summary>移动行为常量 ID</summary>
        public const string BehaviorIdConstant = "Move";

        public override string BehaviorId => BehaviorIdConstant;

        public const double AdjacentDistance = 100;
        public TileId TargetTile { get; private set; }
        public double Progress { get; private set; }
        public long ProcessedWeeks { get; private set; }

        internal MoveBehavior(int planeId, TileId from, TileId target)
            : base(planeId, from)
        {
            TargetTile = target;
        }

        /// <summary>行为推进自己的周进度；到达时请求现有位置系统换格。</summary>
        internal string AdvanceWeek(Npc npc, float speed)
        {
            double next = Progress + Math.Min((double)speed, AdjacentDistance - Progress);
            if (next >= AdjacentDistance && !NpcMgr.Instance.PositionSystem.Arrive(npc.Id, PlaneId, FromTile, TargetTile))
                return "blocked";
            Progress = next;
            ProcessedWeeks = checked(ProcessedWeeks + 1);
            return Progress >= AdjacentDistance ? "arrived" : "moving";
        }

        public MoveBehavior(float duration, List<BehaviorStoryEntry>? storyEntries = null, Rng? rng = null)
            : base(duration, storyEntries, rng)
        {
        }

    }
}
