namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// 战斗场景状态机阶段。
    /// </summary>
    public enum CombatPhase
    {
        /// <summary>空闲，未初始化</summary>
        Idle,
        /// <summary>已初始化（参战者已注册）</summary>
        Initialized,
        /// <summary>预备完成（卡组初始化、Target分配）</summary>
        PreStarted,
        /// <summary>战斗进行中</summary>
        Running,
        /// <summary>战斗结束</summary>
        Finished,
    }

    /// <summary>战场卡牌的延迟变更。保留原有编号，0 曾用于列表重排。</summary>
    public enum CombatFieldChangeType
    {
        /// <summary>移除卡牌。</summary>
        Remove = 1,
        /// <summary>加入卡牌。</summary>
        Add = 2,
        /// <summary>从 Reserve 部署到 Field。</summary>
        Deploy = 3,
        /// <summary>从 Field 召回到 Reserve。</summary>
        Recall = 4
    }

    /// <summary>战斗背包中的滑移方向。整张牌沿该方向移动到阻碍前。</summary>
    public enum CombatCardSlideType
    {
        /// <summary>未指定方向，卡牌保持原位。</summary>
        None,
        /// <summary>向上滑移。</summary>
        Up,
        /// <summary>向下滑移。</summary>
        Down,
        /// <summary>向左滑移。</summary>
        Left,
        /// <summary>向右滑移。</summary>
        Right,
        /// <summary>在整张牌至少能滑动一格的方向中等概率选择一个。</summary>
        Random
    }

    public enum CombatCardPhase
    {
        /// <summary>
        /// 正在等待充灵元
        /// </summary>
        Waiting,
        /// <summary>
        /// 充能完毕，等待扫描
        /// </summary>
        Fulling,
        /// <summary>
        /// 扫描完毕，正在等待生效或者变为拼点
        /// </summary>
        Active,
        /// <summary>
        /// 在待发槽等待拼点
        /// </summary>
        InPending,
        /// <summary>
        /// 正在结算apply中
        /// </summary>
        Applying,
        /// <summary>
        /// 标志使用完成一次，记录使用次数
        /// </summary>
        Finished,
    }

    /// <summary>
    /// 战斗参与者当前状态
    /// </summary>
    public enum CombatantStatus
    {
        /// <summary>正常行动中</summary>
        Active,

        /// <summary>已被击败</summary>
        Defeated,

        /// <summary>已逃离战场</summary>
        Escaped,

        /// <summary>跳过本回合（眩晕/冻结等）</summary>
        Skipped,
        /// <summary>角色已死亡</summary>
        Death
    }
    
    
}
