using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 卡牌修正：纯数值修正容器，无 Lua 脚本行为。
    /// </summary>
    public class CardModifier : IModifierBase
    {
        public string Id { get ; set ; }
        public string SourceId { get ; set  ; }
        public float Duration { get  ; set  ; }
        public float RemainingTime { get  ; set  ; }
        public int MaxStack { get  ; set  ; }
        public int CurrentStack { get  ; set  ; }
        public bool RefreshOnStack { get  ; set  ; }
        public List<StatModifierEntry> StatModifiers { get ; set ; }
        public ExpirePolicy ExpirePolicy { get  ; set  ; }
        public string RemoveTriggerId { get  ; set  ; }

        public static CardModifier CDSpeed(string id,int speed)
        {
            return new CardModifier()
            {
                Id = id,
                Duration = -1,
                RemainingTime = -1,
                MaxStack = 1,
                CurrentStack = speed,
                RefreshOnStack = false,
                ExpirePolicy = ExpirePolicy.Never,
                StatModifiers = new List<StatModifierEntry>()
                {
                    new StatModifierEntry()
                    {
                        StatId = "CDSpeed",
                        Type = ModifierType.Flat,
                        Value = 1,
                        ValuePerStack = 1
                    }
                }

            };
        }
    }
}
