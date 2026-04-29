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

        public MoveBehavior(float duration, List<BehaviorStoryEntry>? storyEntries = null, Rng? rng = null)
            : base(duration, storyEntries, rng)
        {
        }

    }
}
