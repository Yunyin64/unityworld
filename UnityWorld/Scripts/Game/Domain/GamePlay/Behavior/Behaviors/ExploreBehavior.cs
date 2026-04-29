using System.Collections.Generic;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 探索行为：NPC 正在探索周围环境
    /// IsPrimary=true, CanMove=false
    /// </summary>
    public class ExploreBehavior : BehaviorBase
    {
        /// <summary>探索行为常量 ID</summary>
        public const string BehaviorIdConstant = "Explore";

        public override string BehaviorId => BehaviorIdConstant;

        public ExploreBehavior(float duration, List<BehaviorStoryEntry>? storyEntries = null, Rng? rng = null)
            : base(duration, storyEntries, rng)
        {
        }

    }
}
