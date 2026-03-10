namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC相关枚举和常量定�?    /// </summary>
    public static class NpcTypes
    {
        /// <summary>NPC种族/类型</summary>
        public enum NpcType
        {
            Human,
            Monster,
            Animal,
        }

        /// <summary>性别</summary>
        public enum Gender
        {
            Male,
            Female,
            Unknown,
        }

    

        /// <summary>运动状态</summary>
        public enum MoveState
        {
            Idle,        // 静止
            Walking,     // 行走
            Running,     // 奔跑
        }

        /// <summary>
        /// NPC 移动结果
        /// </summary>
        public enum MoveResult
        {
            Success,       // 移动成功
            OutOfBounds,   // 目标格超出位面边界
            Blocked,       // 目标格不可通行（地形阻挡，预留）
            NotFound,      // NPC 位置未注册
        }
    }
}
