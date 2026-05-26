


public enum PracticePath
{
    None,
    Wu,
    Ling,
    Hun
}
        public enum CultivationPointType
        {
           Card,
           Modifier,
           BehaviorCard,
           Story
        }
public enum ContestType
{
    Zhan,
    Da,
    Ci,
    SheJi,
    Shield,
    Block,
    Dodge
}

public enum DamageType
{
    None,
    Zhan,
    Da,
    Ci,
    SheJi,
}

public enum BehaviorStoryTrigger
{
    OnStart,
    OnEnd,
    OnInterrupt,
    OnTimer,
    OnTick
}


    /// <summary>
    /// 元素类型：决定伤害触发的元素反应
    /// </summary>
    public enum BaseElementType
    {
        /// <summary>无元素</summary>
        None,

        /// <summary>火元素</summary>
        Huo,

        /// <summary>水元素</summary>
        Shui,

        /// <summary>金元素</summary>
        Jin,

        /// <summary>木元素</summary>
        Mu,

        /// <summary>土元素</summary>
        Tu,
        /// <summary>拓展元素</summary>
        Extra,
    }

    /// <summary>
    /// Tag 匹配类型
    /// </summary>
    public enum TagMatchType
    {
        /// <summary>严格匹配：候选必须包含 query 的所有 Tag</summary>
        Strict,
        /// <summary>包含匹配：候选覆盖 query Tag 的比例越高权重越大</summary>
        Include,
        /// <summary>权重匹配：基于 Jaccard 相似度（推荐）</summary>
        Weighted,
        /// <summary>自由匹配：不受 query Tag 约束</summary>
        Free,
    }

    // ══════════════════════════════════════════════════════════
    //  战斗相关枚举
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 战斗结束条件类型
    /// </summary>
    public enum CombatEndReason
    {
        /// <summary>战斗尚未结束</summary>
        None,

        /// <summary>某一方全员阵亡</summary>
        AllDefeated,

        /// <summary>达到最大回合数上限</summary>
        TurnLimitReached,

        /// <summary>某方主动投降</summary>
        Surrender,

        /// <summary>某方成功逃跑</summary>
        Escaped,

        /// <summary>特殊事件触发（剧情/条件达成）</summary>
        SpecialCondition,
    }

    /// <summary>
    /// 战斗参与方阵营标识
    /// </summary>
    public enum CombatTeam
    {
        /// <summary>未分配阵营</summary>
        None,
        TeamA,
        TeamB,
        TeamC,
        TeamD,
    }


    /// <summary>
    /// 战斗胜负结果（从某一方视角）
    /// </summary>
    public enum CombatOutcome
    {
        /// <summary>胜利</summary>
        Victory,

        /// <summary>失败</summary>
        Defeat,

        /// <summary>平局（回合上限/双亡等）</summary>
        Draw,

        /// <summary>逃跑（不算胜负）</summary>
        Escaped,
    }

    /// <summary>
    /// 属性修正类型
    /// </summary>
   

/// <summary>事件广播的 Scope 层级（游戏内有限实体类型，按需扩展枚举值）</summary>
public enum Scope
{
    /// <summary>全局层：任何监听者均可收到</summary>
    Global,
    /// <summary>NPC 层：只广播到指定 NPC 身上的监听者</summary>
    Npc,
    /// <summary>地块层：只广播到指定 Tile 身上的监听者</summary>
    Tile,
    /// <summary>位面层：只广播到指定 Plane 身上的监听者</summary>
    Plane,
    Card,
    CombatCard,
    CombatNpc 
}


// ══════════════════════════════════════════════════════════
//  叙事系统相关枚举
// ══════════════════════════════════════════════════════════

/// <summary>
/// 故事触发来源（天地人三池）
/// </summary>
public enum StoryPoolSource
{
    /// <summary>天：宿命池（时间到达触发）</summary>
    Fate,
    /// <summary>地：劫缘池（周期性权重随机触发）</summary>
    Karma,
    /// <summary>人：抉择池（个体主动使用 BehaviorCard 触发）</summary>
    Will,
}

/// <summary>
/// StoryCondition 判断目标类型
/// </summary>
public enum StoryConditionTargetType
{
    /// <summary>NPC 属性值（Stat）</summary>
    NpcStat,
    /// <summary>NPC 是否拥有某 Tag</summary>
    NpcTag,
    /// <summary>NPC 是否拥有某 Trait</summary>
    NpcTrait,
    /// <summary>区域五行元气浓度</summary>
    AuraElement,
    /// <summary>游戏世界时间</summary>
    WorldTime,
    /// <summary>NPC 关系值</summary>
    Relation,
}

/// <summary>
/// StoryCondition 比较运算符
/// </summary>
public enum StoryConditionOperator
{
    /// <summary>大于</summary>
    GreaterThan,
    /// <summary>小于</summary>
    LessThan,
    /// <summary>等于</summary>
    Equal,
    /// <summary>不等于</summary>
    NotEqual,
    /// <summary>大于等于</summary>
    GreaterThanOrEqual,
    /// <summary>小于等于</summary>
    LessThanOrEqual,
    /// <summary>包含（用于 Tag/Trait 检查）</summary>
    Contains,
    /// <summary>不包含</summary>
    NotContains,
}
