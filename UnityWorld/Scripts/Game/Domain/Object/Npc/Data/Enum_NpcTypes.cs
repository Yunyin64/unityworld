namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC相关枚举   /// </summary>
    public static class NpcTypes
    {
        /// <summary>NPC种族/类型</summary>
        public enum NpcType
        {
            /// <summary>人类角色</summary>
            Human,
            /// <summary>妖类角色</summary>
            Monster,
            /// <summary>动物角色</summary>
            Animal,
        }

        /// <summary>性别</summary>
        public enum Gender 
        {
            /// <summary>男性</summary>
            Male,
            /// <summary>女性</summary>
            Female,
            /// <summary>性别未指定</summary>
            Unknown,
        }

    

        /// <summary>运动状态</summary>
        public enum MoveState
        {
            /// <summary>静止状态</summary>
            Idle,        // 静止
            /// <summary>步行状态</summary>
            Walking,     // 行走
            /// <summary>奔跑状态</summary>
            Running,     // 奔跑
        }

        /// <summary>
        /// NPC 移动结果
        /// </summary>
        public enum MoveResult
        {
            /// <summary>寻路成功</summary>
            Success,       // 移动成功
            /// <summary>坐标超出地图范围</summary>
            OutOfBounds,   // 目标格超出位面边界
            /// <summary>地块阻塞</summary>
            Blocked,       // 目标格不可通行（地形阻挡，预留）
            /// <summary>未找到路径</summary>
            NotFound,      // NPC 位置未注册
        }


        
    }
}
