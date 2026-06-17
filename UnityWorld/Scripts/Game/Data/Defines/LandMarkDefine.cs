using System.Text.Json.Serialization;
using UnityWorld.Core;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 地标静态配置模板。
    /// 描述一种地标（自然奇观或后天建筑）的属性、生成条件及对地块的元气影响。
    /// 运行时由 <see cref="LandMarkMgr"/> 实例化为 <see cref="LandMark"/>。
    /// </summary>
    public class LandMarkDefine : DefineBase
    {
        // ── 分类 ──────────────────────────────────────────

        /// <summary>
        /// 是否为自然奇观。
        /// true = 世界原生（计入原初快照）；false = 后天建造（不计入原初快照）。
        /// </summary>
        [JsonPropertyName("IsNatural")]
        public bool IsNatural { get; set; } = true;

        /// <summary>全图唯一（同一 DefineId 最多生成 1 个实例）</summary>
        [JsonPropertyName("IsSingleton")]
        public bool IsSingleton { get; set; } = false;

        // ── 生成规则 ──────────────────────────────────────

        /// <summary>散布生成权重（0 = 不参与独立散布随机；仅在 Step4 生效）</summary>
        [JsonPropertyName("SpawnWeight")]
        public float SpawnWeight { get; set; } = 1f;

        /// <summary>
        /// 允许生成的基础地形列表（空列表 = 不限地形）。
        /// Step4 散布时，只在列表内的地形上尝试生成。
        /// </summary>
        [JsonPropertyName("PlacementTerrains")]
        public List<TerrainType> PlacementTerrains { get; set; } = new();

        /// <summary>
        /// 要求区域持有的拓展地形标签 ID（空列表 = 不限）。
        /// 地块所属 Region 必须包含列表中所有标签，才能在该地块生成此地标。
        /// </summary>
        [JsonPropertyName("PlacementExtraTerrains")]
        public List<string> PlacementExtraTerrains { get; set; } = new();

        // ── 元气影响 ──────────────────────────────────────

        /// <summary>
        /// 该地标对所在地块施加的元气修正 Define ID 列表。
        /// 生成时从 <see cref="TileModifierDefineMgr"/> 取出对应 Define，实例化为 TileModifier 挂载到 Tile。
        /// </summary>
        [JsonPropertyName("ModifierDefineIds")]
        public List<string> ModifierDefineIds { get; set; } = new();
    }
}
