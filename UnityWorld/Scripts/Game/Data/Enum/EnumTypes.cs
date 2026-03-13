
    /// <summary>
    /// 伤害类型：决定伤害受哪种防御减免
    /// </summary>
    public enum DamageType
    {
        /// <summary>受物理防御减免</summary>
        Physical,

        /// <summary>受元素抗性减免</summary>
        Element,

        /// <summary>真实伤害，无视所有防御</summary>
        True,
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
        Jing,

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
    /// 战斗参与者当前状态
    /// </summary>
    public enum CombatantStatus
    {
        /// <summary>正常行动中</summary>
        Active,

        /// <summary>已被击败（HP归零）</summary>
        Defeated,

        /// <summary>已逃离战场</summary>
        Escaped,

        /// <summary>跳过本回合（眩晕/冻结等）</summary>
        Skipped,
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
    public enum ModifierType
    {
        /// <summary>加法修正：最终�?+= Value</summary>
        Flat,

        /// <summary>百分比修正：最终�?*= (1 + Value)</summary>
        Percent,

        /// <summary>强制覆盖：最终值直�?= Value（优先级最高，多个Override取最后一个）</summary>
        Override,

        /// <summary>上限夹紧：最终�?= min(最终�? Value)</summary>
        ClampMax,

        /// <summary>下限夹紧：最终�?= max(最终�? Value)</summary>
        ClampMin,
    }

/// <summary>事件广播的 Scope 层级（游戏内有限实体类型，按需扩展枚举值）</summary>
public enum EventScope
{
    /// <summary>全局层：任何监听者均可收到</summary>
    Global,
    /// <summary>NPC 层：只广播到指定 NPC 身上的监听者</summary>
    Npc,
    /// <summary>地块层：只广播到指定 Tile 身上的监听者</summary>
    Tile,
    /// <summary>位面层：只广播到指定 Plane 身上的监听者</summary>
    Plane,
}

/// <summary>旧版全局事件枚举，已废弃，请改用字符串 eventId + EventDefine 机制</summary>
[System.Obsolete("Em_Event 已废弃，请使用 EventDefine 字符串 ID 体系")]
public enum Em_Event
{
    
}
