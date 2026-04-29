namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 基础地形类型枚举。
    /// 共 7 种固定地形，由算法感知用于地形生成与五行浓度映射。
    /// 策划如需扩展叙事语义标签，请使用 <see cref="UnityWorld.Game.Data.ExtraTerrainDefine"/>。
    /// </summary>
    public enum TerrainType
    {
        /// <summary>平原：五行均衡，适宜农耕与门派建立</summary>
        Plain = 0,

        /// <summary>丘陵：土属性偏高，地势稳固</summary>
        Hill = 1,

        /// <summary>山地：金属性偏高，矿脉丰富</summary>
        Mountain = 2,

        /// <summary>河湖：水属性偏高，灵气流动</summary>
        RiverLake = 3,

        /// <summary>海洋：水属性极高，深不可测</summary>
        Ocean = 4,

        /// <summary>荒漠：火属性偏高，炎热干燥</summary>
        Desert = 5,

        /// <summary>森林：木属性偏高，生机旺盛</summary>
        Forest = 6,
    }
}
