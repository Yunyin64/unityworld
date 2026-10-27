namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 修炼节点定义：功法中每个解锁点的奖励配置
    /// </summary>
    public class CultivationPointDefine
    {
        /// <summary>解锁阈值（修炼点数达到此值时解锁）</summary>
        public int Threshold { get; set; } = 0;

        /// <summary>奖励类型</summary>
        public CultivationPointType Type { get; set; } = CultivationPointType.Card;

        /// <summary>引用 ID（Card/BehaviorCard/Modifier/Story 的定义 ID）</summary>
        public string RefId { get; set; } = "";
    }


    /// <summary>
    /// 功法定义：包含道途类型、境界等级、修炼节点序列、核心效果
    /// </summary>
    public class CultivationDefine : DefineBase
    {
        public int BackpackWidth { get; set; } = 5;
        public int BackpackHeight { get; set; } = 5;
        /// <summary>显式等级/几何测试布局优先；正式默认单格主功法固定公开 (3,3)。</summary>
        public List<BackpackLevelDefine> BackpackLevels { get; set; } = [];

        public BackpackLevelDefine GetBackpackDefinition(int level)
        {
            if (BackpackLevels.Count > 0)
            {
                var matching = BackpackLevels.Where(d => d.Level == level).ToList();
                if (matching.Count != 1) throw new InvalidOperationException($"功法 {ID} 等级 {level} 必须有唯一背包布局。");
                return matching[0].Clone();
            }
            if (level != 1) throw new InvalidOperationException($"功法 {ID} 没有等级 {level} 的背包布局。");
            return new BackpackLevelDefine { Width = BackpackWidth, Height = BackpackHeight, MainCardCells = [BackpackCoordinates.ToCell(new(3, 3))] };
        }

        /// <summary>功法描述</summary>
        public string Desc { get; set; } = "";

        /// <summary>所属道途类型</summary>
        public PracticePath PathType { get; set; } = PracticePath.None;

        /// <summary>适用境界等级（0 表示通用，可跨境界修炼）</summary>
        public int RealmLevel { get; set; } = 0;

        /// <summary>修炼点数上限（功法修炼满需要的总点数）</summary>
        public int MaxPoint { get; set; } = 100;

        /// <summary>完整度（功法完整性，0-1，影响修炼效果）</summary>
        public float Completeness { get; set; } = 1.0f;

        /// <summary>修炼节点序列</summary>
        public CultivationPointDefine[] Points { get; set; } = [];
    }
}
