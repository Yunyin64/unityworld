using System.Collections.Generic;
using UnityWorld.Core;
using UnityWorld.Game.World;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 行为子系统：管理 NPC 的行为槽（主行为 + 次要行为）
    /// 负责：注册、添加、打断、Tick 推进、自然结束、便捷查询
    /// </summary>
    public class NpcSystemBehavior : NpcSystemBase<NpcBehaviorData>
    {
        protected override Dictionary<int, NpcBehaviorData> _dataTable { get; set; } = new();

        public void Clear() => _dataTable.Clear();


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
            // 周移动由世界结算持有；原来的秒持续时间不能自然结束它。
            if (data.PrimaryBehavior != null && data.PrimaryBehavior is not MoveBehavior { IsWeeklyManaged: true })
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
            WorldMgr.EnsureMutable();
            if (!_dataTable.TryGetValue(npcId, out var data))
            {
                LogMgr.Instance.Warn("[NpcSystemBehavior] AddPrimary 找不到 NPC {0}", npcId);
                return false;
            }

            if (data.PrimaryBehavior != null)
            {
                LogMgr.Instance.Warn("[NpcSystemBehavior] NPC {0} 已有主行为 {1}，无法添加新行为", 
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
            WorldMgr.EnsureMutable();
            if (!_dataTable.TryGetValue(npcId, out var data)) return;
            if (data.PrimaryBehavior == null) return;
            // 普通打断入口不能让NPC绕过途中锁定；受阻／注销由内部提交清理。
            if (data.PrimaryBehavior is MoveBehavior { IsWeeklyManaged: true })
                throw new InvalidOperationException("周移动尚未到达，不能换目标或打断。");

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

        /// <summary>内部无故事回调提交；周管理模式只占用主行为槽，不结算秒故事。</summary>
        private void SetWeeklyMove(int id, MoveBehavior next)
        {
            if (!_dataTable.TryGetValue(id, out var data)) throw new InvalidOperationException("NPC行为数据缺失。");
            if (next != null) next.Ownerint = id;
            data.PrimaryBehavior = next;
            NpcMgr.Instance.PositionSystem.SetMoving(id, next != null);
        }

        public NpcActionSnapshot GetAction(int id)
        {
            var position = NpcMgr.Instance.PositionSystem.GetPosition(id);
            var primary = GetPrimary(id);
            var move = primary as MoveBehavior;
            bool weekly = move?.IsWeeklyManaged == true;
            return new(id, position != null, position?.PlaneId, position?.TileId,
                weekly ? "move" : primary == null ? "idle" : "busy",
                weekly ? move.FromTile : null, weekly ? move.TargetTile : null,
                weekly ? move.Progress : 0, weekly ? MoveBehavior.AdjacentDistance : 0);
        }

        /// <summary>真实移动启动入口，未来手动选择也可复用；不推进日期或移动进度。</summary>
        public bool TryStartMove(int id, TileId target)
        {
            WorldMgr.EnsureMutable();
            if (GetPrimary(id) != null) return false;
            var position = NpcMgr.Instance.PositionSystem.GetPosition(id);
            if (position == null || target == position.TileId ||
                !NpcMgr.Instance.PositionSystem.GetMoveCandidates(id).Contains(target)) return false;
            SetWeeklyMove(id, new MoveBehavior(position.PlaneId, position.TileId, target));
            return true;
        }

        // 预留移动策略扩展点：未来根据npc和合法候选填写target。
        // 现在方法体保持为空，target不赋值；外层使用独立的随机选格逻辑。
        // 这里只选目标，不改位置、不推进时间，填入的目标仍需外层校验。
        private void SelectMoveTargetByStrategy(Npc npc, IReadOnlyList<TileId> candidates, ref TileId? target)
        {
            // TODO: 未来在这里填写策略；当前不设置target，继续使用外层随机选择。
        }

        internal void StopWeeklyMove(int id)
        {
            if (GetPrimary(id) is MoveBehavior { IsWeeklyManaged: true }) SetWeeklyMove(id, null);
        }

        private TileId ChooseRandomMoveTarget(Plane plane, NpcPositionData position, long week, IReadOnlyList<TileId> candidates)
            => candidates[plane.NpcSystem.CreateRandom(position.BirthOrder, week, 2).Range(0, candidates.Count)];

        /// <summary>位面遍历到NPC后，使用原主行为槽决定／推进这一周。</summary>
        internal NpcWeekResult OnWeek(Plane plane, int id, long week)
        {
            var mgr = NpcMgr.Instance;
            var before = GetAction(id);
            var position = mgr.PositionSystem.GetPosition(id);
            var npc = mgr.GetById(id);
            var primary = GetPrimary(id);
            var move = primary as MoveBehavior;
            bool weekly = move?.IsWeeklyManaged == true;
            NpcWeekResult Result(string kind, float speed = 0, TileId? target = null, double progress = 0, string reason = "")
                => new(id, position?.BirthOrder ?? -1, kind, target ?? before.Target, speed,
                    before, GetAction(id), progress, reason);

            if (npc == null || position == null || position.PlaneId != plane.Id.Value || !mgr.BioSystem.GetData(id).IsAlive)
            {
                if (weekly) SetWeeklyMove(id, null);
                return Result("skipped", reason: "NPC不存在、死亡或不属于此位面。");
            }
            if (plane.State != PlaneTypes.PlaneState.Active) return Result("skipped", reason: "位面未激活。");
            if (primary != null && !weekly) return Result("busy", reason: "已有其他主行为。");
            if (!plane.TileSystem.CanWalk(position.TileId) || weekly && (move.PlaneId != position.PlaneId ||
                move.FromTile != position.TileId || !plane.TileSystem.CanWalk(move.TargetTile)))
            {
                if (weekly) SetWeeklyMove(id, null);
                return Result("blocked", progress: weekly ? move.Progress : 0, reason: "来源或目标已失效。");
            }
            float speed;
            try { speed = mgr.PositionSystem.GetActionSpeed(npc); }
            catch (Exception ex) { return Result("invalid_state", progress: weekly ? move.Progress : 0, reason: ex.Message); }
            if (!weekly)
            {
                var candidates = mgr.PositionSystem.GetMoveCandidates(id);
                TileId? selected = null;
                SelectMoveTargetByStrategy(npc, candidates, ref selected);
                var target = selected ?? ChooseRandomMoveTarget(plane, position, week, candidates);
                if (!candidates.Contains(target)) return Result("decision_error", speed, target, reason: "目标不在合法候选中。");
                if (target == position.TileId) return Result("waited", speed, target);
                move = new MoveBehavior(position.PlaneId, position.TileId, target);
                SetWeeklyMove(id, move);
            }
            string outcome = move.AdvanceWeek(npc, speed);
            if (outcome != "moving") SetWeeklyMove(id, null);
            return Result(outcome, speed, move.TargetTile, move.Progress,
                outcome == "blocked" ? "目标无法到达。" : "");
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
