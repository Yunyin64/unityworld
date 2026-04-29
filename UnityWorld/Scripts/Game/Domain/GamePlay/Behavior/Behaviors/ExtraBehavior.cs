using System.Collections.Generic;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 通用拓展行为类：由 ExtraBehaviorDefine 数据驱动创建的任意行为变体
    /// BehaviorId 由构造参数传入，对应 ExtraBehaviorDefine.ID
    /// </summary>
    public class ExtraBehavior : BehaviorBase
    {
        private readonly string _behaviorId;

        public override string BehaviorId => _behaviorId;

        /// <summary>
        /// 构造拓展行为
        /// </summary>
        /// <param name="behaviorId">行为 ID（对应 ExtraBehaviorDefine.ID）</param>
        /// <param name="duration">持续时间</param>
        /// <param name="storyEntries">Story 触发规则列表</param>
        /// <param name="rng">随机数生成器</param>
        public ExtraBehavior(string behaviorId, float duration, List<BehaviorStoryEntry>? storyEntries = null, Rng? rng = null)
            : base(duration, storyEntries, rng)
        {
            _behaviorId = behaviorId;
        }

    }
}
