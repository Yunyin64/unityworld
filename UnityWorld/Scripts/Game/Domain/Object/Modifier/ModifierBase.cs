using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 过期策略：描述 Modifier 在什么条件下被判定为"应移除"。
    /// </summary>
    public enum ExpirePolicy
    {
        /// <summary>永不自动过期，只能手动移除</summary>
        Never,

        /// <summary>RemainingTime ≤ 0 时过期</summary>
        TimeBased,

        /// <summary>CurrentStack ≤ 0 时过期</summary>
        StackBased,

        /// <summary>时间到 或 层数归零，任一满足即过期</summary>
        TimeOrStack,

        /// <summary>纯靠 RemoveTriggerId 指定的事件直接移除，不靠轮询判定</summary>
        TriggerBased,
    }

    /// <summary>
    /// 修正源基类：所有能对游戏实体施加持续性修正的来源（地标、NPC、卡牌等）均继承此类。
    /// </summary>
    public interface IModifierBase
    {
        /// <summary>修正源唯一标识</summary>
        public string Id { get; set; }

        /// <summary>来源实体标识（例如 NPC Id、地标 Id）</summary>
        public string SourceId { get; set; }

        /// <summary>持续时间（Tick）；-1 表示永久有效</summary>
        public float Duration { get; set; } 

        /// <summary>剩余时间（Tick）；永久修正时此值无意义</summary> 
        public float RemainingTime { get; set; }

        /// <summary>最大叠加层数（1 = 不叠加，0 = 无上限）</summary>
        int MaxStack { get; set; }
    
        /// <summary>当前层数</summary>
        int CurrentStack { get; set; }
    
        /// <summary>叠加时是否刷新 Duration</summary>
        bool RefreshOnStack { get; set; }

        /// <summary>属性修正列表</summary>
        public List<StatModifierEntry> StatModifiers { get; set; }

        /// <summary>过期策略</summary>
        ExpirePolicy ExpirePolicy { get; set; }

        /// <summary>移除触发器 ID，引用 TriggerDefine.ID。为空表示不响应触发器事件。</summary>
        string RemoveTriggerId { get; set; }
    }

    /// <summary>
    /// IModifierBase 扩展方法：IsExpired 统一判定、ReduceStack / AddStack 层数操作。
    /// </summary>
    public static class IModifierBaseExt
    {
        /// <summary>
        /// 基于 ExpirePolicy 的统一过期判定。
        /// </summary>
        public static bool IsExpired(this IModifierBase self) => self.ExpirePolicy switch
        {
            ExpirePolicy.Never      => false,
            ExpirePolicy.TimeBased  => self.Duration > 0 && self.RemainingTime <= 0f,
            ExpirePolicy.StackBased => self.CurrentStack <= 0,
            ExpirePolicy.TimeOrStack => (self.Duration > 0 && self.RemainingTime <= 0f)
                                        || self.CurrentStack <= 0,
            ExpirePolicy.TriggerBased => false,
            _ => false,
        };

        /// <summary>
        /// 减少层数，不会低于 0。
        /// </summary>
        public static void ReduceStack(this IModifierBase self, int count = 1)
        {
            self.CurrentStack = Math.Max(0, self.CurrentStack - count);
        }

        /// <summary>
        /// 增加层数，受 MaxStack 限制（MaxStack == 0 表示无上限）。
        /// RefreshOnStack 为 true 且 Duration > 0 时重置 RemainingTime。
        /// </summary>
        public static void AddStack(this IModifierBase self, int count = 1)
        {
            int newStack = self.CurrentStack + count;
            if (self.MaxStack > 0 && newStack > self.MaxStack)
            {
                newStack = self.MaxStack;
            }
            self.CurrentStack = newStack;

            if (self.RefreshOnStack && self.Duration > 0)
            {
                self.RemainingTime = self.Duration;
            }
        }
    }
    
}
