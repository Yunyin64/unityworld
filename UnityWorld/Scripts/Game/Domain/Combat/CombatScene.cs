using UnityWorld.Core;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// 战斗场景：纯实例对象，完全离线模拟，不依赖任何全局 Tick。
    /// 调用方负责构造、驱动、销毁，并将 CombatResult 回写大世界。
    ///
    /// Tick驱动模型：
    ///   每张卡独立计时器，并行前进。
    ///   CD就绪→攻防卡入待发槽→槽满挤出→直击/双方对拼。
    ///   每Tick检查SP溢出战斗结束条件。
    ///
    /// 标准用法：
    ///   var scene = new CombatScene(rng);
    ///   scene.Init(participants, maxTicks);
    ///   scene.PreStart();
    ///   scene.Start();
    ///   while (!scene.IsFinished) scene.Tick();
    ///   var result = scene.GetResult();
    /// </summary>
    public partial class CombatScene:GameEntityBase
    {

        // ── 参战者 ────────────────────────────────────────────

        /// <summary>全部参战者列表</summary>
        private Dictionary<int,CombatNpc> Combatants = new();
        // ── 环境参数 ──────────────────────────────────────────

        /// <summary>Tick数上限（超过则强制结束，防止死循环）</summary>
        public int MaxTicks { get; private set; } = 100;

        /// <summary>当前已进行的总Tick数</summary>
        public int CurrentTick { get; private set; } = 0;

        // ── 状态机 ────────────────────────────────────────────

        private CombatPhase _phase = CombatPhase.Idle;

        /// <summary>战斗是否已结束</summary>
        public bool IsFinished => _phase == CombatPhase.Finished;

        // ── 结果 ──────────────────────────────────────────────

        private CombatResult _result;
        // ── 事件监听追踪（End 时清理）─────────────────────────
        private readonly List<(string eventId, ScopeKey scope, IEventListener listener)> _registeredListeners = [];

        // ══════════════════════════════════════════════════════
        //  Phase 1 : Init
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// 初始化战斗场景：从大世界 Npc 实例注册参战者。
        /// 保存 Npc 引用用于战斗结算回写。
        /// </summary>
        /// <param name="npcParticipants">大世界 Npc + 阵营列表</param>
        /// <param name="maxTicks">Tick上限，默认100</param>
        public void Init(
            IEnumerable<(Npc npc, CombatTeam team)> npcParticipants,
            int maxTicks = 100)
        {
            AssertPhase(CombatPhase.Idle, nameof(Init));

            MaxTicks = maxTicks;

            var participantList = npcParticipants.ToList();
            Combatants = participantList
                .Select(p => CombatNpc.CreateCombatNpc(p.npc))
                .ToDictionary(c => c.Id, c => c);

            // 快照战前卡组（用于战后识别新增伤势卡）


            _phase = CombatPhase.Initialized;
        }


        // ══════════════════════════════════════════════════════
        //  Phase 2 : PreStart
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// 战斗预备：快照HP、初始化CardStates、分配初始Target、触发战前被动。
        /// 从大世界 Npc 读取真实数据（HP/SP/MP/卡组/五行亲和）。
        /// </summary>
        public void PreStart()
        {
            AssertPhase(CombatPhase.Initialized, nameof(PreStart));

            foreach (var c in Combatants.Values)
            {
                // 尝试从大世界 Npc 读取数据
                var worldNpc = c.Owner;

                if (worldNpc != null)
                {
                    c.PreStart();
                }

            }


            _phase = CombatPhase.PreStarted;
            Log("PreStart完成，Target已确定。");

        }

        // ════════════════════════════════════════════════════
        //  Phase 3 : Start
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// 战斗开始：校验CardStates。
        /// </summary>
        public void Start()
        {
            AssertPhase(CombatPhase.PreStarted, nameof(Start));

            foreach (var c in Combatants.Values)
            {
                c.Start();
            }
            _phase = CombatPhase.Running;
            Log("战斗开始！");
        }

        // ══════════════════════════════════════════════════════
        //  Phase 4 : Tick（主循环）
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// 推进一个 Tick。
        /// 调用方应在 while(!scene.IsFinished) 中循环调用。
        /// </summary>
        public void Tick()
        {
            AssertPhase(CombatPhase.Running, nameof(Tick));

            foreach (var c in Combatants.Values) c.DoManaDraw();
            foreach (var c in Combatants.Values) c.UseCard();
            foreach (var c in Combatants.Values) c.ProcessContest();
            foreach (var c in Combatants.Values) c.DealDamage();
            foreach (var c in Combatants.Values) c.DealCardDeckChange();
            foreach (var c in Combatants.Values) c.CheckDefeated();
            CheckEndConditions();

            // Step 8: 定期快照（每 50 Tick = 5 秒）
            if (CurrentTick % 50 == 0 && !IsFinished)
            {
                Log(string.Format("── 快照 ── {0}Ticks", CurrentTick));
            }
            CurrentTick++;
            foreach (var c in Combatants.Values) c.Tick();
        }
        public CombatResult Run(bool needLog = false)
        {
            // PreStart → Start
            this.PreStart();
            this.Start();

            // Tick 循环
            while (!this.IsFinished)
                this.Tick();

            // 导出日志
            
            // 清理
            this.Cleanup();

            return _result;
        }

        // ══════��═══════════════════════════════════════════════
        //  状态机辅助（供 Handler 调用）
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// 设置状态机为 Finished（供 SpilloverHandler 调用）。
        /// </summary>
        internal void SetPhaseFinished() => _phase = CombatPhase.Finished;

        /// <summary>
        /// 清理战斗场景（清理事件监听、Lua 环境、重置静态 Logger）。
        /// </summary>
        public void Cleanup()
        {
            // 清理事件监听
            foreach (var (eventId, scope, listener) in _registeredListeners)
                EventMgr.Instance?.RemoveEvent(eventId, scope, listener);
            _registeredListeners.Clear();

            // 清理 Lua 卡牌环境
            LuaMgr.Instance?.UnloadAllCardScripts();

        }

        // ══════════════════════════════════════════════════════

        /// <summary>校验当前阶段，不符则抛出异常</summary>
        private void AssertPhase(CombatPhase expected, string methodName)
        {
            if (_phase != expected)
                throw new InvalidOperationException(
                    $"CombatScene.{methodName} 需要阶段 [{expected}]，当前为 [{_phase}]。");
        }

        /// <summary>
        /// 检查并触发战斗结束条件。
        /// </summary>
        public void CheckEndConditions()
        {
            if (IsFinished) return;

            // 条件1：Tick 上限
            if (CurrentTick >= MaxTicks)
            {
                EndCombat(CombatEndReason.TurnLimitReached);
                return;
            }

            // 条件2：场上只剩一方存活
            var activeTeams = Combatants.Values
                .Where(c => c.IsActive)
                .Select(c => c.Team)
                .Distinct()
                .ToList();

            if (activeTeams.Count <= 1)
            {
                EndCombat(CombatEndReason.AllDefeated);
            }
        }

        /// <summary>
        /// 执行战斗结束结算，生成 CombatResult。
        /// </summary>
        private CombatResult EndCombat(CombatEndReason reason)
        {
            SetPhaseFinished();

            var survivors = Combatants.Values.Where(c => c.IsActive).ToList();
            CombatTeam winner = CombatTeam.None;
            if (survivors.Count > 0)
            {
                var winnerTeams = survivors.Select(c => c.Team).Distinct().ToList();
                if (winnerTeams.Count == 1)
                    winner = winnerTeams[0];
            }

            var combatantResults = Combatants.Values.Select(c => new CombatantResult
            {
                NpcId = c.Id,
                Team = c.Team,
                FinalStatus = c.Status,
            }).ToList();

            var result = CombatResult.CombatSceneResult(reason,winner,CurrentTick,combatantResults);
            CombatScene.Log($"\n战斗结束！原因={reason}，胜者={winner}，共{CurrentTick} Tick。");

            return result;
        }

        
        /// <summary>
        /// 触发战斗域事件（通过 EventMgr）。
        /// </summary>
        public  static void TriggerCombatEvent(string eventId, CombatNpc npc, object args)
        {
            EventMgr.Instance?.TriggerEvent(
                eventId,
                args,
                (Scope.CombatNpc, npc.Id.ToString()));
        }
        public static void Log(string msg)
        {
            
        }
        public override void LogAllInfo()
        {
             
        }

        public override string ToString()
        {
            return "";
        }
    }
}
