using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    public enum StackReduceType
    {
        Tick,
        Stack,
        OnUse
    }

    /// <summary>
    /// 卡牌修正
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
        public StackReduceType StackReduceType { get ; set ; }
        public bool isExpired => RemainingTime >= Duration && CurrentStack == 0;

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
