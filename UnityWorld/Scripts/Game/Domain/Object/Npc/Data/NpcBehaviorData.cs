using System.Collections.Generic;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 行为运行时数据：主行为槽 + 次要行为列表
    /// PrimaryBehavior 为 null 表示空闲，可以使用 BehaviorCard
    /// </summary>
    public class NpcBehaviorData : IDomainDataBase
    {
        /// <summary>主行为（唯一，null=空闲）</summary>
        public BehaviorBase PrimaryBehavior { get; set; }

        /// <summary>次要行为列表（可多个，V1 预留结构）</summary>
        public List<BehaviorBase> SecondaryBehaviors { get; set; } = new();

        /// <summary>
        /// 是否空闲（主行为为 null）
        /// </summary>
        public bool IsIdle => PrimaryBehavior == null;

        public NpcBehaviorData Clone()
        {
            var copy = (NpcBehaviorData)MemberwiseClone();
            copy.SecondaryBehaviors = new List<BehaviorBase>(SecondaryBehaviors);
            return copy;
        }
        IDomainDataBase IDomainDataBase.Clone() => Clone();

        /// <summary>
        /// 日志输出
        /// </summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("┌── Behavior · 行为状态 ─────────────────────");
            LogMgr.Instance.Dbg("│  主行为:        {0}", PrimaryBehavior?.ToString() ?? "无");
            LogMgr.Instance.Dbg("│  次要行为:    {0}", SecondaryBehaviors.ToInfoString());
            LogMgr.Instance.Dbg("└───────────────────────────────────────────");
        }
    }

    /// <summary>
    /// Npc partial 类：行为数据便捷访问器
    /// </summary>
    public partial class Npc
    {
        public BehaviorBase GetPrimaryBehavior() => BehaviorData.PrimaryBehavior;
        public List<BehaviorBase> GetSecondaryBehaviors() => BehaviorData.SecondaryBehaviors;

    }
}
