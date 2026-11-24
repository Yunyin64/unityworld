#nullable enable annotations
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

        /// <summary>探索行为的类型标识</summary>
        public override string BehaviorId => BehaviorIdConstant;

        /// <summary>本格周探索。行为卡仍通过下方按秒数构造的入口启动。</summary>
        internal ExploreBehavior(int planeId, TileId from) : base(planeId, from)
        {
        }

        /// <summary>调度器已核对角色仍在启动时的位面与格子，一周只结算这一次本地遭遇。</summary>
        internal (string? EventId, string? GrantedCardId) AdvanceWeek(Plane plane, Rng rng)
            => ExploreEncounterData.Resolve(plane, FromTile, Ownerint, rng);

        /// <summary>创建带时长和可选剧情的秒制探索行为</summary>
        public ExploreBehavior(float duration, List<BehaviorStoryEntry>? storyEntries = null, Rng? rng = null)
            : base(duration, storyEntries, rng)
        {
        }

    }
}
