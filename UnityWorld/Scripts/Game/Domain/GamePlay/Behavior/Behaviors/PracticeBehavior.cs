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

        /// <summary>修炼行为的类型标识</summary>
        public override string BehaviorId => BehaviorIdConstant;

        /// <summary>创建带时长和可选剧情的秒制修炼行为</summary>
        public PracticeBehavior(float duration, List<BehaviorStoryEntry>? storyEntries = null, Rng? rng = null)
            : base(duration, storyEntries, rng)
        {
        }

    }
}
