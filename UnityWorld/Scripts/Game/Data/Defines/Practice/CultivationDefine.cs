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
