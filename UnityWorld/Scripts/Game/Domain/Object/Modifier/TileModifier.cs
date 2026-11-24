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
        /// <summary>地块状态编号</summary>
        public string Id { get  ; set  ; }
        /// <summary>地块状态来源编号</summary>
        public string SourceId { get  ; set  ; }
        /// <summary>配置的持续时间</summary>
        public float Duration { get  ; set  ; }
        /// <summary>当前剩余持续时间</summary>
        public float RemainingTime { get  ; set  ; }
        /// <summary>状态附带的属性修正</summary>
        public List<StatModifierEntry> StatModifiers { get ; set ; }
        /// <summary>状态层数上限</summary>
        public int MaxStack { get  ; set  ; }
        /// <summary>当前状态层数</summary>
        public int CurrentStack { get  ; set  ; }
        /// <summary>叠加时是否重置持续时间</summary>
        public bool RefreshOnStack { get  ; set  ; }
        /// <summary>状态过期方式</summary>
        public ExpirePolicy ExpirePolicy { get  ; set  ; }
        /// <summary>触发移除的事件编号</summary>
        public string RemoveTriggerId { get  ; set  ; }

        /// <summary>校验持续时间并复制元气变化量，创建地块状态</summary>
        public TileModifier(string id, string sourceId, TileAura auraData, float duration = -1f)   
        {
            ArgumentNullException.ThrowIfNull(auraData);
            if (!float.IsFinite(duration) || duration < -1f)
                throw new ArgumentOutOfRangeException(nameof(duration));
            Id = id;
            SourceId = sourceId;
            Duration = duration;
            RemainingTime = duration;
            AuraData = new TileAura
            {
                Jin = auraData.Jin, Mu = auraData.Mu, Shui = auraData.Shui,
                Huo = auraData.Huo, Tu = auraData.Tu,
            };
            StatModifiers = new();
            MaxStack = CurrentStack = 1;
            RemoveTriggerId = string.Empty;
            ExpirePolicy = duration > 0f ? ExpirePolicy.TimeBased : ExpirePolicy.Never;
        }
    }
}
