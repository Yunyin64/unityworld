namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 修正源基类：所有能对游戏实体施加持续性修正的来源（地标、NPC、卡牌等）均继承此类。
    /// </summary>
    public abstract class ModifierBase
    {
        /// <summary>修正源唯一标识</summary>
        public string Id { get; set; }

        /// <summary>来源实体标识（例如 NPC Id、地标 Id）</summary>
        public string SourceId { get; set; }

        /// <summary>持续时间（秒）；-1 表示永久有效</summary>
        public float Duration { get; set; } = -1f;

        /// <summary>剩余时间（秒）；永久修正时此值无意义</summary>
        public float RemainingTime { get; set; }

        /// <summary>
        /// 是否已过期：Duration == -1 时永不过期；否则 RemainingTime &lt;= 0 时过期
        /// </summary>
        public bool IsExpired => Duration != -1f && RemainingTime <= 0f;

        protected ModifierBase(string id, string sourceId, float duration = -1f)
        {
            Id = id;
            SourceId = sourceId;
            Duration = duration;
            RemainingTime = duration;
        }
    }
}
