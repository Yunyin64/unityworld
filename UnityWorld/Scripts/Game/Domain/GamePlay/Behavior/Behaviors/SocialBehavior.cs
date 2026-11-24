using System.Collections.Generic;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 社交行为：NPC 正在进行社交活动
    /// IsPrimary=true, CanMove=false
    /// </summary>
    public class SocialBehavior : BehaviorBase
    {
        /// <summary>社交行为常量 ID</summary>
        public const string BehaviorIdConstant = "Social";

        /// <summary>社交行为的类型标识</summary>
        public override string BehaviorId => BehaviorIdConstant;

        /// <summary>创建带时长和可选剧情的秒制社交行为</summary>
        public SocialBehavior(float duration, List<BehaviorStoryEntry>? storyEntries = null, Rng? rng = null)
            : base(duration, storyEntries, rng)
        {
        }
    }
}
