namespace UnityWorld.Game.World
{
    /// <summary>
    /// 世界时间管理：全局物理时间，匀速推进，不受任何加速/减缓影响
    /// NPC的主观时间由 NpcSystemBio 中的 TimeFlowRate 单独处理
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

        /// <summary>
        /// 推进世界时间（由 WorldMgr.Tick 调用）
        /// </summary>
        public static void Advance(float deltaTime)
        {
            DeltaTime = deltaTime;
            TotalTime += deltaTime;
            CurrentTick++;
        }

        /// <summary>
        /// 初始化世界时间
        /// </summary>
        public static void Initialize(int randomSeed = 0)
        {
            CurrentTick = 0;
            DeltaTime = 0f;
            TotalTime = 0f;
            RandomSeed = randomSeed;
        }
    }
}