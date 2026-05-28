using System.Collections.Generic;
using UnityWorld.Core;
namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 行为 Story 触发规则运行时数据
    /// 用于 BehaviorBase 内部，描述在行为生命周期的何时机触发哪个 Story
    /// </summary>
    public class BehaviorStoryEntry:GameEntityBase
    {
        /// <summary>Story 定义 ID（直接指定模式）</summary>
        public string StoryId { get; set; } = "";

        /// <summary>Story Tag 列表（TagBag 匹配模式，StoryId 为空时使用）</summary>
        public List<string> StoryTags { get; set; } = new();

        /// <summary>触发时机</summary>
        public BehaviorStoryTrigger Trigger { get; set; }

        /// <summary>触发概率（OnTick 用，0.0~1.0，每 Tick 概率）</summary>
        public float Chance { get; set; } = 1.0f;

        /// <summary>延迟时间（OnTimer 用，行为 ElapsedTime 达到此值时触发）</summary>
        public float Delay { get; set; } = 0f;

        /// <summary>是否已触发（OnTimer 用，一次性标记）</summary>
        public bool HasTriggered { get; set; } = false;

        /// <summary>
        /// 默认构造
        /// </summary>
        public BehaviorStoryEntry() { }

        /// <summary>
        /// 便捷构造：指定 StoryId 和 Trigger
        /// </summary>
        public BehaviorStoryEntry(string storyId, BehaviorStoryTrigger trigger)
        {
            StoryId = storyId;
            Trigger = trigger;
        }

        /// <summary>
        /// 便捷构造：指定 StoryTags 和 Trigger
        /// </summary>
        public BehaviorStoryEntry(List<string> storyTags, BehaviorStoryTrigger trigger)
        {
            StoryTags = storyTags;
            Trigger = trigger;
        }

        /// <summary>
        /// 完整构造
        /// </summary>
        public BehaviorStoryEntry(string storyId, List<string> storyTags, BehaviorStoryTrigger trigger, float chance = 1.0f, float delay = 0f)
        {
            StoryId = storyId;
            StoryTags = storyTags ?? new List<string>();
            Trigger = trigger;
            Chance = chance;
            Delay = delay;
        }

        public override void LogAllInfo()
        {
                LogMgr.Instance.Dbg("│  StoryEntry: Trigger={0}, StoryId='{1}', StoryTags={2}, Chance={3}, Delay={4}, HasTriggered={5}",
                    Trigger, StoryId, StoryTags.ToInfoString(), Chance, Delay, HasTriggered);
        }

        public override string ToString()
        {
            return $"(Trigger={Trigger}, StoryId='{StoryId}')";
        }
    }
}
