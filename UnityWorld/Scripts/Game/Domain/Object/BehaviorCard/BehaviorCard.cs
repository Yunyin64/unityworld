namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 行为卡运行时实例：个体持有的可主动使用的行为意图卡
    /// 通过 BehaviorCardMgr 统一管理
    /// </summary>
    public class BehaviorCard
    {
        /// <summary>引用的行为卡静态定义 ID</summary>
        public string DefineId { get; set; } = "";

        /// <summary>持有者 NPC ID</summary>
        public int OwnerId { get; set; }

        /// <summary>已使用次数</summary>
        public int UsageCount { get; set; } = 0;

        public BehaviorCard() { }

        public BehaviorCard(string defineId, int ownerId)
        {
            DefineId = defineId;
            OwnerId  = ownerId;
        }
    }
}