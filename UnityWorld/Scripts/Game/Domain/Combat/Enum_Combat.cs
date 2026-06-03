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

        public enum ComabtFieldChangeType
        {
            Displace,
            Remove,
            Add,
            /// <summary>从 Reserve 部署到 Field</summary>
            Deploy,
            /// <summary>从 Field 召回到 Reserve</summary>
            Recall
        }
        public enum ComabtCardDisplaceType
        {
            None,
            First,
            Last,
            Random
        }

    public enum CombatCardPhase
    {
        /// <summary>
        /// 有CD，但在等待开始走
        /// </summary>
        Waiting,
        /// <summary>
        /// 有CD，正在走
        /// </summary>
        InCD,
        /// <summary>
        /// 有CD，走完了
        /// </summary>
        CDFull,
        /// <summary>
        /// 标志使用完成一次，记录使用次数
        /// </summary>
        Finished,
        /// <summary>被动无CD</summary>
        Passive,
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
        Death
    }
    
    
}
