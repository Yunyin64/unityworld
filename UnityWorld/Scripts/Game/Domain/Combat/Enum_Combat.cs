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
        /// <summary>预备完成（HP快照、卡组初始化、Target分配）</summary>
        PreStarted,
        /// <summary>战斗进行中</summary>
        Running,
        /// <summary>战斗结束</summary>
        Finished,
    }

        public enum ComabtCardDeckChangeType
        {
            Displace,
            Remove,
            Add
        }
        public enum ComabtCardDisplaceType
        {
            None,
            First,
            Last,
            Random
        }

    public enum CardType
    {
        FaShu,
        ZhaoShi, 
        FaBao ,
        Wound,
        Item,
        ShenTong
    }
    public enum CombatCardPhase
    {
        WaitResource,
        InCD,
        Ready,
        InPending,
        Finished,
        /// <summary>准备移除</summary>
        IsExpired,
        /// <summary>被动模式：不参与 CD 循环，由 Keyword 驱动</summary>
        Passive,
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
        Death
    }
    
    
}
