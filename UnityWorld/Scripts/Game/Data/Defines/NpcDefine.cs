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
        /// <summary>NPC 种族</summary>
        public NpcTypes.NpcType NpcType { get; set; } = NpcTypes.NpcType.Human;

        /// <summary>初始社会角色列表（对应 SocialRoles.json 中的 id，可多个）</summary>
        public string[] DefaultRoles { get; set; } = [];

        /// <summary>初始年龄最小值（单位：年）</summary>
        public float InitAgeMin { get; set; } = 18f;

        /// <summary>初始年龄最大值（单位：年）</summary>
        public float InitAgeMax { get; set; } = 40f;

        /// <summary>寿元上限基础值（单位：年）</summary>
        public float BaseLifespanMax { get; set; } = 80f;

        /// <summary>移动速度基础值</summary>
        public float BaseMoveSpeed { get; set; } = 3f;

        /// <summary>初始修为等级最小值（0 = 凡人）</summary>
        public int InitCultivationLevelMin { get; set; } = 0;

        /// <summary>初始修为等级最大值（0 = 凡人）</summary>
        public int InitCultivationLevelMax { get; set; } = 0;

        /// <summary>
        /// NPC 出生时携带的初始 Trait 列表（对应 TraitDefine.Id）。
        /// 这是模板默认值，运行时可通过 NpcSystemTrait 动态增减。
        /// </summary>
        public string[] InitialTraits { get; set; } = [];

        // ── 内置配置工厂（开发期使用，未来可从 JSON 加载替代）──────────

        /// <summary>人类农民</summary>
        public static NpcDefine HumanFarmer() => new NpcDefine
        {
            ID = "human_farmer",
            NpcType = NpcTypes.NpcType.Human,
            DefaultRoles = ["farmer"],
            InitAgeMin = 15f,
            InitAgeMax = 50f,
            BaseLifespanMax = 70f,
            BaseMoveSpeed = 2.5f,
        };

        /// <summary>修士游侠</summary>
        public static NpcDefine CultivatorWanderer() => new NpcDefine
        {
            ID = "cultivator_wanderer",
            NpcType = NpcTypes.NpcType.Human,
            DefaultRoles = ["cultivator", "wanderer"],
            InitAgeMin = 16f,
            InitAgeMax = 30f,
            BaseLifespanMax = 500f,
            BaseMoveSpeed = 5f,
            InitCultivationLevelMin = 1,
            InitCultivationLevelMax = 5,
        };
    }
}
