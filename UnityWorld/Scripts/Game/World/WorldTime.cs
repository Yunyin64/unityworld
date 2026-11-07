namespace UnityWorld.Game.World
{
    /// <summary>
    /// 世界时间入口：保留旧物理秒时钟，另提供显式推进的游戏周历。
    /// 两者没有自动换算关系；周历不负责对象行为或结算顺序。
    /// </summary>
    public static class WorldTime
    {
        /// <summary>当前物理Tick总数</summary>
        public static int CurrentTick { get; private set; }

        /// <summary>本Tick的物理时间步长（秒）</summary>
        public static float DeltaTime { get; private set; }

        /// <summary>从游戏开始到现在的累计物理时间（秒）</summary>
        public static float TotalTime { get; private set; }

        /// <summary>随机种子（在WorldMgr初始化时设定）</summary>
        public static int RandomSeed { get; private set; }

        // 周历实例随成功初始化替换，避免旧世界的通知订阅流入新世界。
        public static WorldCalendar Calendar { get; private set; } = new();

        /// <summary>当前游戏日期的不可变快照，读取不会推进时间。</summary>
        public static WorldDate CurrentDate => Calendar.CurrentDate;

        /// <summary>仅推进游戏周历一周，不增加物理秒数，也不驱动管理器 Tick。</summary>
        public static WorldCalendarTransition AdvanceWeek() => Calendar.AdvanceWeek();

        /// <summary>
        /// 推进旧物理秒时钟（由 WorldMgr.Tick 调用），不推进游戏周历。
        /// </summary>
        public static void Advance(float deltaTime)
        {
            DeltaTime = deltaTime;
            TotalTime += deltaTime;
            CurrentTick++;
        }

        /// <summary>
        /// 初始化两种时钟。WorldMgr 只在全部世界加载成功后调用，失败时保留旧时间。
        /// </summary>
        public static void Initialize(int randomSeed = 0)
        {
            CurrentTick = 0;
            DeltaTime = 0f;
            TotalTime = 0f;
            RandomSeed = randomSeed;
            Calendar = new WorldCalendar();
        }
    }
}
