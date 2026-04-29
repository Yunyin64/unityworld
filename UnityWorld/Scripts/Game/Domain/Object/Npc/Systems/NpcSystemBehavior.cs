using System.Collections.Generic;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 行为子系统：管理 NPC 的行为槽（主行为 + 次要行为）
    /// 负责：注册、添加、打断、Tick 推进、自然结束、便捷查询
    /// </summary>
    public class NpcSystemBehavior : NpcSystemBase<NpcBehaviorData>
    {
        protected override Dictionary<int, NpcBehaviorData> _dataTable { get; set; } = new();


        /// <summary>
        /// NPC 诞生时：创建空行为数据（初始为空闲状态）
        /// </summary>
        public override void OnEntityBorn(BirthContext context)
        {
            var npc = context.MainNpc;
            Register(npc, new NpcBehaviorData());
        }

        // ── Tick 推进 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 每 Tick 推进行为时间，结算 Story，处理自然结束
        /// </summary>
        public override void OnTick(Npc npc, float deltaTime)
        {
            if (!_dataTable.TryGetValue(npc.Id, out var data)) return;

            // 推进主行为
            if (data.PrimaryBehavior != null)
            {
                data.PrimaryBehavior.OnTick(deltaTime);

                // 检查自然结束
                if (data.PrimaryBehavior.IsFinished)
                {
                    data.PrimaryBehavior.OnEnd();
                    data.PrimaryBehavior = null;
                }
            }

            // 推进次要行为（V1 结构预留）
            for (int i = data.SecondaryBehaviors.Count - 1; i >= 0; i--)
            {
                var secondary = data.SecondaryBehaviors[i];
                secondary.OnTick(deltaTime);

                if (secondary.IsFinished)
                {
                    secondary.OnEnd();
                    data.SecondaryBehaviors.RemoveAt(i);
                }
            }
        }

        // ── 主行为管理 ───────────────────────────────────────────────────────

        /// <summary>
        /// 添加主行为（仅当当前为空闲时）
        /// </summary>
        /// <returns>是否成功添加</returns>
        public bool AddPrimary(int npcId, BehaviorBase behavior)
        {
            if (!_dataTable.TryGetValue(npcId, out var data))
            {
                LogMgr.Warn("[NpcSystemBehavior] AddPrimary 找不到 NPC {0}", npcId);
                return false;
            }

            if (data.PrimaryBehavior != null)
            {
                LogMgr.Warn("[NpcSystemBehavior] NPC {0} 已有主行为 {1}，无法添加新行为", 
                    npcId, data.PrimaryBehavior.BehaviorId);
                return false;
            }

            behavior.Ownerint = npcId;
            data.PrimaryBehavior = behavior;
            behavior.OnStart();
            return true;
        }

        /// <summary>
        /// 打断当前主行为
        /// </summary>
        public void InterruptPrimary(int npcId)
        {
            if (!_dataTable.TryGetValue(npcId, out var data)) return;
            if (data.PrimaryBehavior == null) return;

            data.PrimaryBehavior.OnInterrupt();
            data.PrimaryBehavior = null;
        }

        /// <summary>
        /// 获取当前主行为
        /// </summary>
        public BehaviorBase? GetPrimary(int npcId)
        {
            return _dataTable.TryGetValue(npcId, out var data) ? data.PrimaryBehavior : null;
        }

        /// <summary>
        /// 是否空闲（主行为为 null）
        /// </summary>
        public bool IsIdle(int npcId)
        {
            return _dataTable.TryGetValue(npcId, out var data) && data.PrimaryBehavior == null;
        }

        // ── 官便便捷查询 API ───────────────────────────────────────────────────

        /// <summary>主行为是否为 MoveBehavior</summary>
        public bool IsMoving(int npcId) => GetPrimary(npcId) is MoveBehavior;

        /// <summary>主行为是否为 PracticeBehavior</summary>
        public bool IsPracticing(int npcId) => GetPrimary(npcId) is PracticeBehavior;

        /// <summary>主行为是否为 ExploreBehavior</summary>
        public bool IsExploring(int npcId) => GetPrimary(npcId) is ExploreBehavior;

        /// <summary>主行为是否为 SocialBehavior</summary>
        public bool IsSocializing(int npcId) => GetPrimary(npcId) is SocialBehavior;

        /// <summary>主行为的 BehaviorId 是否匹配</summary>
        public bool IsInBehavior(int npcId, string behaviorId)
        {
            var primary = GetPrimary(npcId);
            return primary != null && primary.BehaviorId == behaviorId;
        }

        // ── 次要行为管理（V1 预留）─────────────────────────────────────────────

        /// <summary>添加次要行为</summary>
        public void AddSecondary(int npcId, BehaviorBase behavior)
        {
            if (!_dataTable.TryGetValue(npcId, out var data)) return;
            
            behavior.Ownerint = npcId;
            data.SecondaryBehaviors.Add(behavior);
            behavior.OnStart();
        }

        /// <summary>移除次要行为</summary>
        public void RemoveSecondary(int npcId, string behaviorId)
        {
            if (!_dataTable.TryGetValue(npcId, out var data)) return;

            for (int i = data.SecondaryBehaviors.Count - 1; i >= 0; i--)
            {
                if (data.SecondaryBehaviors[i].BehaviorId == behaviorId)
                {
                    data.SecondaryBehaviors[i].OnInterrupt();
                    data.SecondaryBehaviors.RemoveAt(i);
                }
            }
        }

        /// <summary>获取次要行为列表</summary>
        public IReadOnlyList<BehaviorBase> GetSecondaries(int npcId)
        {
            return _dataTable.TryGetValue(npcId, out var data) 
                ? data.SecondaryBehaviors.AsReadOnly() 
                : (IReadOnlyList<BehaviorBase>)new List<BehaviorBase>();
        }

    }
}
