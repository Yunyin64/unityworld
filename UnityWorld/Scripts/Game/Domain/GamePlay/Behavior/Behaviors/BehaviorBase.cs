using System.Collections.Generic;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 行为抽象基类：NPC"正在做什么"的状态容器
    /// 包含生命周期管理（OnStart/OnTick/OnEnd/OnInterrupt）和 Story 结算引擎
    /// </summary>
    public abstract class BehaviorBase:GameEntityBase
    {
        // ── 核心属性 ─────────────────────────────────────────────────────────────

        /// <summary>行为类型标识（"Move"/"Practice"/"Explore"/"Social" 或自定义 ID）</summary>
        public abstract string BehaviorId { get; }

        /// <summary>是否为主行为（主行为唯一、不可移动；次要行为可多个、可移动）</summary>
        public virtual bool IsPrimary => true;

        /// <summary>是否允许移动（主行为永远 false，次要行为可 true）</summary>
        public virtual bool CanMove => false;

        // ── 运行时状态 ─────────────────────────────────────────────────────────────

        /// <summary>行为持续时间（外部传入，如 BehaviorCardDefine.BehaviorDuration）</summary>
        public float Duration { get; protected set; }

        /// <summary>已消耗时间（Tick 累加）</summary>
        public float ElapsedTime { get; protected set; }

        /// <summary>是否已结束（ElapsedTime >= Duration）</summary>
        public bool IsFinished => ElapsedTime >= Duration;

        /// <summary>Story 触发规则列表</summary>
        public List<BehaviorStoryEntry> StoryEntries { get; protected set; } = new();

        /// <summary>持有者 NPC ID（由 NpcSystemBehavior 在 AddPrimary 时设置）</summary>
        public int Ownerint { get; set; }

        /// <summary>随机数生成器（由外部传入，保证可复现）</summary>
        protected Rng? _rng;

        // ── 构造 ─────────────────────────────────────────────────────────────────

        protected BehaviorBase(float duration, List<BehaviorStoryEntry>? storyEntries = null, Rng? rng = null)
        {
            Duration = duration;
            StoryEntries = storyEntries ?? new List<BehaviorStoryEntry>();
            _rng = rng;
        }

        // ── 生命周期 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 行为开始时调用：结算 OnStart 类型的 StoryEntry
        /// </summary>
        public virtual void OnStart()
        {
            EvaluateStoryEntries(BehaviorStoryTrigger.OnStart);
            LogMgr.Instance.Dbg("[Behavior] OnStart: {0} (npc={1}, duration={2})", BehaviorId, Ownerint, Duration);
        }

        /// <summary>
        /// 行为每 Tick 调用：推进时间、结算 OnTick/OnTimer 类型的 StoryEntry
        /// </summary>
        public virtual void OnTick(float deltaTime)
        {
            ElapsedTime += deltaTime;

            // 结算 OnTick（概率触发）
            EvaluateOnTickEntries();

            // 结算 OnTimer（定时触发）
            EvaluateOnTimerEntries();
        }

        /// <summary>
        /// 行为自然结束时调用：结算 OnEnd 类型的 StoryEntry
        /// </summary>
        public virtual void OnEnd()
        {
            EvaluateStoryEntries(BehaviorStoryTrigger.OnEnd);
            LogMgr.Instance.Dbg("[Behavior] OnEnd: {0} (npc={1}, elapsed={2}/{3})", BehaviorId, Ownerint, ElapsedTime, Duration);
        }

        /// <summary>
        /// 行为被打断时调用：结算 OnInterrupt 类型的 StoryEntry，广播事件
        /// </summary>
        public virtual void OnInterrupt()
        {
            EvaluateStoryEntries(BehaviorStoryTrigger.OnInterrupt);

            // 广播行为打断事件
            EventMgr.Instance?.TriggerEvent(
                "BehaviorInterrupted",
                new { npcId = Ownerint, BehaviorId = BehaviorId, ElapsedTime = ElapsedTime },
                (Scope.Npc, Ownerint.ToString())
            );

            LogMgr.Instance.Dbg("[Behavior] OnInterrupt: {0} (npc={1}, elapsed={2})", BehaviorId, Ownerint, ElapsedTime);
        }

        // ── Story 结算引擎 ───────────────────────────────────────────────────────

        /// <summary>
        /// 结算指定触发时机的所有 StoryEntry
        /// </summary>
        protected void EvaluateStoryEntries(BehaviorStoryTrigger trigger)
        {
            foreach (var entry in StoryEntries)
            {
                if (entry.Trigger != trigger) continue;
                ExecuteStoryEntry(entry);
            }
        }

        /// <summary>
        /// 结算 OnTick 类型的 StoryEntry（概率触发）
        /// </summary>
        protected void EvaluateOnTickEntries()
        {
            foreach (var entry in StoryEntries)
            {
                if (entry.Trigger != BehaviorStoryTrigger.OnTick) continue;

                // 概率判定
                if (_rng != null && entry.Chance < 1.0f)
                {
                    if (_rng.Range(0f, 1f) >= entry.Chance) continue;
                }

                ExecuteStoryEntry(entry);
            }
        }

        /// <summary>
        /// 结算 OnTimer 类型的 StoryEntry（定时触发，仅一次）
        /// </summary>
        protected void EvaluateOnTimerEntries()
        {
            foreach (var entry in StoryEntries)
            {
                if (entry.Trigger != BehaviorStoryTrigger.OnTimer) continue;
                if (entry.HasTriggered) continue;

                // 时间判定
                if (ElapsedTime >= entry.Delay)
                {
                    ExecuteStoryEntry(entry);
                    entry.HasTriggered = true;
                }
            }
        }

        /// <summary>
        /// 执行单个 StoryEntry：走 StoryId 直接触发 或 StoryTags TagBag 匹配
        /// </summary>
        protected void ExecuteStoryEntry(BehaviorStoryEntry entry)
        {
            var subject = NpcMgr.Instance?.GetById(Ownerint);

            if (!string.IsNullOrEmpty(entry.StoryId))
            {
                // 直接指定模式
                StoryMgr.Instance?.TriggerStory(entry.StoryId, subject, StoryPoolSource.Will, _rng);
            }
            else if (entry.StoryTags.Count > 0)
            {
                // TagBag 匹配模式
                StoryMgr.Instance?.TriggerStoryByTags(entry.StoryTags, subject, StoryPoolSource.Will, _rng);
            }
        }

        public override string ToString()
        {
            return $"{BehaviorId})";
        }

        public  override void LogAllInfo()
        {
            LogMgr.Instance.Dbg("│ ── 行为 ───────────────────────────────────");
            LogMgr.Instance.Dbg("│  行为ID:     {0}", BehaviorId);
            LogMgr.Instance.Dbg("│  所属NPC:    {0}", Ownerint);
            LogMgr.Instance.Dbg("│  主行为:     {0}  可移动: {1}", IsPrimary, CanMove);
            LogMgr.Instance.Dbg("│  进度:       {0:F1}/{1:F1}  已结束: {2}", ElapsedTime, Duration, IsFinished);

            if (StoryEntries.Count > 0)
            {
                LogMgr.Instance.Dbg("│  故事条目 ({0}):", StoryEntries.Count);
                foreach (var e in StoryEntries)
                {
                    var target = !string.IsNullOrEmpty(e.StoryId)
                        ? e.StoryId
                        : $"Tags[{string.Join(",", e.StoryTags)}]";
                    LogMgr.Instance.Dbg("│    {0} | {1} | 概率={2:P0} 延迟={3:F1} 已触发={4}",
                        e.Trigger, target, e.Chance, e.Delay, e.HasTriggered);
                }
            }
        }
    }
}
