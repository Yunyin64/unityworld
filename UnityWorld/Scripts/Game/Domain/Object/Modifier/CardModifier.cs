using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 卡牌修正：纯数值修正容器，无 Lua 脚本行为。
    /// </summary>
    public class CardModifier : IModifierBase
    {
        /// <summary>卡牌状态编号</summary>
        public string Id { get ; set ; }
        /// <summary>施加状态的来源编号</summary>
        public string SourceId { get ; set  ; }
        /// <summary>状态初始持续时间</summary>
        public float Duration { get  ; set  ; }
        /// <summary>状态剩余持续时间</summary>
        public float RemainingTime { get  ; set  ; }
        /// <summary>状态层数上限</summary>
        public int MaxStack { get  ; set  ; }
        /// <summary>状态当前层数</summary>
        public int CurrentStack { get  ; set  ; }
        /// <summary>叠加时是否重置持续时间</summary>
        public bool RefreshOnStack { get  ; set  ; }
        /// <summary>状态提供的属性修正</summary>
        public List<StatModifierEntry> StatModifiers { get ; set ; }
        /// <summary>状态过期方式</summary>
        public ExpirePolicy ExpirePolicy { get  ; set  ; }
        /// <summary>触发移除的事件编号</summary>
        public string RemoveTriggerId { get  ; set  ; }

        /// <summary>创建在用卡事件后移除的冷却速度修正</summary>
        public static CardModifier CDSpeed(string id,int speed)
        {
            return new CardModifier()
            {
                Id = id,
                Duration = -1,
                RemainingTime = -1,
                MaxStack = 99,
                CurrentStack = 1,
                RefreshOnStack = false,
                ExpirePolicy = ExpirePolicy.TriggerBased,
                RemoveTriggerId = "OnUse",
                StatModifiers = new List<StatModifierEntry>()
                {
                    new StatModifierEntry()
                    {
                        StatId = "CDSpeed",
                        Type = ModifierType.Flat,
                        Value = speed,
                    }
                }

            };
        }

        /// <summary>创建限时冷却速度上限为零的旧式冻结修正</summary>
        public static CardModifier Freeze(string id,int time)
        {
            return new CardModifier()
            {
                Id = id,
                Duration = time,
                RemainingTime = time,
                MaxStack = 99,
                CurrentStack = 1,
                RefreshOnStack = false,
                ExpirePolicy = ExpirePolicy.TimeBased,
                StatModifiers = new List<StatModifierEntry>()
                {
                    new StatModifierEntry()
                    {
                        StatId = "CDSpeed",
                        Type = ModifierType.ClampMax,
                        Value = -100,
                    }
                }

            };
        }

        /// <summary>创建持续至战斗结束的固定值属性修正</summary>
        public static CardModifier StatBuff(string id, string statId, float value)
        {
            return new CardModifier()
            {
                Id = id,
                Duration = -1,
                RemainingTime = -1,
                MaxStack = 1,
                CurrentStack = 1,
                RefreshOnStack = false,
                ExpirePolicy = ExpirePolicy.Never,
                StatModifiers = new List<StatModifierEntry>()
                {
                    new StatModifierEntry()
                    {
                        StatId = statId,
                        Type = ModifierType.Flat,
                        Value = value,
                    }
                }
            };
        }
    }
}
