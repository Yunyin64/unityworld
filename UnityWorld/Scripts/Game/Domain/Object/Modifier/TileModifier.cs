namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 地块修正源：持有五行元气变化量，每 Tick 将 AuraData 累积到目标 Tile 的 CurrentAura。
    /// AuraData 的语义为「每秒变化量」，由 TileSystemAura 按 deltaTime 比例累积。
    /// </summary>
    public class TileModifier : ModifierBase
    {
        /// <summary>每秒元气变化量（复用 TileAura 作数据容器，语义为 delta/s）</summary>
        public TileAura AuraData { get; set; }

        public TileModifier(string id, string sourceId, TileAura auraData, float duration = -1f)
            : base(id, sourceId, duration)
        {
            AuraData = auraData;
        }
    }
}
