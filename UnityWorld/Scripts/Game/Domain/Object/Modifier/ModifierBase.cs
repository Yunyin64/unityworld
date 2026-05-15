using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
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
        public List<StatModifierEntry> StatModifiers { get; set; }

    }
    public static class IModifierBaseExt
    {   
        public static bool IsExpired(this IModifierBase self) => self.Duration > 0 && self.RemainingTime <= 0f;
    }
    
}
