using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 地块修正源：持有五行元气变化量，每 Tick 将 AuraData 累积到目标 Tile 的 CurrentAura。
    /// AuraData 的语义为「每秒变化量」，由 TileSystemAura 按 deltaTime 比例累积。
    /// </summary>
    public class TileModifier : IModifierBase
    {
        /// <summary>每秒元气变化量（复用 TileAura 作数据容器，语义为 delta/s）</summary>
        public TileAura AuraData { get; set; }
        public string Id { get  ; set  ; }
        public string SourceId { get  ; set  ; }
        public float Duration { get  ; set  ; }
        public float RemainingTime { get  ; set  ; }
        public List<StatModifierEntry> StatModifiers { get ; set ; }
        public int MaxStack { get  ; set  ; }
        public int CurrentStack { get  ; set  ; }
        public bool RefreshOnStack { get  ; set  ; }
        public ExpirePolicy ExpirePolicy { get  ; set  ; }
        public string RemoveTriggerId { get  ; set  ; }

        public TileModifier(string id, string sourceId, TileAura auraData, float duration = -1f)   
        {
            AuraData = auraData;
        }
    }
}
