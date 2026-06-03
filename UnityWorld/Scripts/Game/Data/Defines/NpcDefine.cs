using UnityWorld.Game.Domain;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// NPC 定义/配置模板（由策划配置，用于批量创建同类 NPC）
    /// 模板只描述 NPC 在生成时的初始属性范围，运行时由 NpcMgr 随机化
    /// </summary>
    public class NpcDefine:DefineBase
    {
        public string Surname { get; set; } = "";
        public string GivenName { get; set; } = "";
        public string DaoTitle { get; set; } = "";
        public NpcTypes.Gender Gender { get; set; } = NpcTypes.Gender.Male;
        /// <summary>NPC 种族</summary>
        public NpcTypes.NpcType NpcType { get; set; } = NpcTypes.NpcType.Human;

        /// <summary>初始年龄（单位：年）</summary>
        public float InitAge { get; set; } = 18f;

        /// <summary>初始修为等级（0 = 凡人）</summary>
        public int InitCultivationLevel { get; set; } = 0;

        /// <summary>
        /// NPC 出生时携带的初始 Trait 列表（对应 TraitDefine.Id）。
        /// 这是模板默认值，运行时可通过 NpcSystemTrait 动态增减。
        /// </summary>
        public string[] InitialTraits { get; set; } = [];
        public string[] InitCardDeck { get; set; } = [];

        public Dictionary<string,int> InitStat  { get; set; } = new();


    }
}
