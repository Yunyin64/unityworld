using System.Collections.Generic;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 闭关修炼行为：NPC 正在闭关修炼
    /// IsPrimary=true, CanMove=false
    /// </summary>
    public class PracticeBehavior : BehaviorBase
    {
        /// <summary>修炼行为常量 ID</summary>
        public const string BehaviorIdConstant = "Practice";

        public override string BehaviorId => BehaviorIdConstant;

        public PracticeBehavior(float duration, List<BehaviorStoryEntry>? storyEntries = null, Rng? rng = null)
            : base(duration, storyEntries, rng)
        {
        }

    }
}
