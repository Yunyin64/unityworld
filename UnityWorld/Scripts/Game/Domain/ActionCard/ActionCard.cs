namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 行为卡运行时实例：个体持有的可主动使用的行为意图卡
    /// 通过 ActionCardMgr 统一管理
    /// </summary>
    public class ActionCard
    {
        /// <summary>引用的行为卡静态定义 ID</summary>
        public string DefineId { get; set; } = "";

        /// <summary>持有者 NpcId</summary>
        public NpcId OwnerId { get; set; }

        /// <summary>已使用次数</summary>
        public int UsageCount { get; set; } = 0;

        public ActionCard() { }

        public ActionCard(string defineId, NpcId ownerId)
        {
            DefineId = defineId;
            OwnerId  = ownerId;
        }
    }
}
