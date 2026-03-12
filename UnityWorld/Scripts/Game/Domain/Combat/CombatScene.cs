using UnityWorld.Game.Common.Math;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// 战斗场景：纯实例对象，完全离线模拟，不依赖任何全局 Tick。
    /// 调用方负责构造、驱动、销毁，并将 CombatResult 回写大世界。
    ///
    /// 出招表模型：
    ///   每个 NPC 持有有序出招表（DeckSequence），按顺序循环出招，
    ///   无随机摸牌。每次对抗双方出招指针均推进一位。
    ///
    /// 标准用法：
    ///   var scene = new CombatScene(rng);
    ///   scene.Init(participants, maxTurns);
    ///   scene.PreStart();
    ///   scene.Start();
    ///   while (!scene.IsFinished) scene.NextTurn();
    ///   var result = scene.GetResult();
    /// </summary>
    public class CombatScene
    {
        // ── 外部依赖 ──────────────────────────────────────────

        private readonly Rng _rng;

        // ── 参战者 ────────────────────────────────────────────

        /// <summary>全部参战者列表（按行动顺序排列后固定）</summary>
        private List<CombatNpc> _combatants = [];

        /// <summary>当前回合行动者的索引（在 _combatants 中循环）</summary>
        private int _turnIndex = 0;

        // ── 环境参数 ──────────────────────────────────────────

        /// <summary>回合数上限（超过则强制结束，防止死循环）</summary>
        public int MaxTurns { get; private set; } = 100;

        /// <summary>当前已进行的总回合数</summary>
        public int CurrentTurn { get; private set; } = 0;

        // ── 状态机 ────────────────────────────────────────────

        private CombatPhase _phase = CombatPhase.Idle;

        public bool IsFinished => _phase == CombatPhase.Finished;

        // ── 结果 ──────────────────────────────────────────────

        private CombatResult? _result;

        // ── 构造 ──────────────────────────────────────────────

        public CombatScene(Rng rng)
        {
            _rng = rng;
        }

        // ══════════════════════════════════════════════════════
        //  Phase 1 : Init
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// 初始化战斗场景：注册参战者，设置环境参数。
        /// </summary>
        /// <param name="participants">参战者信息列表（NpcId + 阵营）</param>
        /// <param name="maxTurns">回合上限，默认100</param>
        public void Init(
            IEnumerable<(NpcId id, CombatTeam team)> participants,
            int maxTurns = 100)
        {
            AssertPhase(CombatPhase.Idle, nameof(Init));

            MaxTurns = maxTurns;

            _combatants = participants
                .Select(p => new CombatNpc(p.id, p.team))
                .ToList();

            _phase = CombatPhase.Initialized;

            Log($"Init完成，参战者{_combatants.Count}人，上限{MaxTurns}回合。");
        }

        // ══════════════════════════════════════════════════════
        //  Phase 2 : PreStart
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// 战斗预备：快照HP、构建出招表、排列行动顺序、分配初始Target、触发战前被动。
        /// </summary>
        public void PreStart()
        {
            AssertPhase(CombatPhase.Initialized, nameof(PreStart));

            foreach (var c in _combatants)
            {
                // 1. 快照 HP（占位：修为等级×100，待接入真实 StatId.HpMax）
                float hp = SnapshotHp(c);
                c.SnapshotHp(hp);

                // 2. 构建有序出招表（占位：空序列，待接入 CardSystemDeck）
                c.DeckSequence = BuildDeckSequence(c);

                // 3. 重置出招指针
                c.ResetDeckPointer();

                Log($"  [{c.Id.Value}] HP快照={hp}, 出招表={c.DeckSequence.Count}张");
            }

            // 4. 按速度降序排列行动顺序（占位：用修为等级代替速度）
            _combatants = _combatants
                .OrderByDescending(c => c.Stats.Get(StatId.CultivationLevel))
                .ToList();

            // 5. 分配初始 Target（占位：各指向第一个可见的敌方存活者）
            AssignInitialTargets();

            // 6. 触发战前被动（预留，待接入 Trait/Effect 系统）
            OnPreStartPassives();

            _phase = CombatPhase.PreStarted;
            Log("PreStart完成，行动顺序与Target已确定。");
        }

        // ══════════════════════════════════════════════════════
        //  Phase 3 : Start
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// 战斗开始：校验出招表，推入第一回合。
        /// </summary>
        public void Start()
        {
            AssertPhase(CombatPhase.PreStarted, nameof(Start));

            foreach (var c in _combatants)
            {
                if (c.DeckSequence.Count == 0)
                    Log($"  [警告] [{c.Id.Value}] 出招表为空，该参战者将无法出招。");
                else
                    Log($"  [{c.Id.Value}] 出招表{c.DeckSequence.Count}张，就绪。");
            }

            _turnIndex = 0;
            _phase = CombatPhase.Running;
            Log("战斗开始！");
        }

        // ══════════════════════════════════════════════════════
        //  Phase 4 : NextTurn（循环调用）
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// 推进一个行动单位的回合。
        /// 调用方应在 while(!scene.IsFinished) 中循环调用。
        /// </summary>
        public void NextTurn()
        {
            AssertPhase(CombatPhase.Running, nameof(NextTurn));

            // 找到下一个可行动者（跳过已败/已逃）
            var actor = GetNextActor();
            if (actor == null)
            {
                EndCombat(CombatEndReason.AllDefeated);
                return;
            }

            CurrentTurn++;
            Log($"\n── 第{CurrentTurn}回合 [{actor.Id.Value} / {actor.Team}] ──");

            // 确保 Target 有效（若已失效则重新分配）
            ReassignTargetIfNeeded(actor);

            var target = actor.Target;

            if (target != null && actor.DeckSequence.Count > 0)
            {
                // 1. 取双方当前出招
                var actorCard  = actor.CurrentCard;
                var targetCard = target.CurrentCard;

                //Log($"  [{actor.Id.Value}] 出招[{actorCard?.Id ?? "空"}]  vs  [{target.Id.Value}] 应招[{targetCard?.Id ?? "空"}]");

                // 2. 执行卡与卡的结算（规则待定，当前占位：造成固定伤害）
                ExecuteCardVsCard(actor, actorCard, target, targetCard);

                // 3. 双方出招指针各推进一位
                actor.AdvanceDeckIndex();
                target.AdvanceDeckIndex();

                if (actor.CurrentDeckIndex == 0)
                    Log($"  [{actor.Id.Value}] 完成第{actor.CycleCount}次循环。");
                if (target.CurrentDeckIndex == 0)
                    Log($"  [{target.Id.Value}] 完成第{target.CycleCount}次循环。");
            }
            else
            {
                Log($"  [{actor.Id.Value}] 无目标或出招表为空，跳过。");
            }

            actor.TurnsActed++;

            // 4. 推进行动索引
            AdvanceTurnIndex();

            // 5. 检查结束条件
            CheckEndConditions();
        }

        // ══════════════════════════════════════════════════════
        //  Phase 5 : End → GetResult
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// 获取战斗结果。只能在 IsFinished == true 后调用。
        /// </summary>
        public CombatResult GetResult()
        {
            if (!IsFinished || _result == null)
                throw new InvalidOperationException("战斗尚未结束，无法获取结果。");
            return _result;
        }

        // ══════════════════════════════════════════════════════
        //  内部逻辑
        // ══════════════════════════════════════════════════════

        /// <summary>从 StatBlock 读取参战者的初始 HP（局部实现）</summary>
        private static float SnapshotHp(CombatNpc c)
        {
            // TODO: 接入真实 HP Stat（如 StatId.HpMax）
            // 当前占位：修为等级×100，最低100
            float level = c.Stats.Get(StatId.CultivationLevel, 1f);
            return Math.Max(100f, level * 100f);
        }

        /// <summary>构建参战者的有序出招表（局部实现）</summary>
        private static List<Card.CardData> BuildDeckSequence(CombatNpc c)
        {
            // TODO: 接入 CardSystemDeck，按 NPC 主题 Tag 构建有序出招表
            return [];
        }

        /// <summary>分配初始 Target（占位：指向第一个敌方存活者）</summary>
        private void AssignInitialTargets()
        {
            foreach (var c in _combatants)
            {
                c.Target = FindEnemyTarget(c);
               // Log($"  [{c.Id.Value}] Target → [{c.Target?.Id.Value ?? "无"}]");
            }
        }

        /// <summary>若当前 Target 已失效则重新分配</summary>
        private void ReassignTargetIfNeeded(CombatNpc actor)
        {
            if (actor.Target == null || !actor.Target.IsActive)
            {
                actor.Target = FindEnemyTarget(actor);
                if (actor.Target != null)
                    Log($"  [{actor.Id.Value}] Target重分配 → [{actor.Target.Id.Value}]");
            }
        }

        /// <summary>找到指定 NPC 的一个可攻击敌方目标（局部实现）</summary>
        private CombatNpc? FindEnemyTarget(CombatNpc actor)
        {
            // TODO: 细化目标分配策略（距离、威胁值等）
            return _combatants.FirstOrDefault(c => c.Team != actor.Team && c.IsActive);
        }

        /// <summary>触发战前被动（预留）</summary>
        private static void OnPreStartPassives()
        {
            // TODO: 遍历参战者 Trait，触发 CombatStart 类型的被动效果
        }

        /// <summary>
        /// 执行卡与卡的结算（占位：主动方对目标造成固定伤害，被动方的牌暂不产生效果）
        /// TODO: 接入完整卡与卡结算规则
        /// </summary>
        private void ExecuteCardVsCard(
            CombatNpc actor,   Card.CardData? actorCard,
            CombatNpc target,  Card.CardData? targetCard)
        {
            // 占位：造成固定伤害，忽略被动方的牌
            float damage = 10f + actor.Stats.Get(StatId.CultivationLevel) * 5f;
            target.ApplyDamage(damage);
            Log($"  [{actor.Id.Value}] → [{target.Id.Value}] 受到{damage}伤害，剩余HP={target.CurrentHp}"
                + (target.IsActive ? "" : " 【已阵亡】"));
        }

        /// <summary>获取下一个可行动的参战者（跳过非 Active 状态）</summary>
        private CombatNpc? GetNextActor()
        {
            int count = _combatants.Count;
            for (int i = 0; i < count; i++)
            {
                var c = _combatants[(_turnIndex + i) % count];
                if (c.IsActive) return c;
            }
            return null;
        }

        /// <summary>推进行动索引到下一位</summary>
        private void AdvanceTurnIndex()
        {
            _turnIndex = (_turnIndex + 1) % _combatants.Count;
        }

        /// <summary>检查并触发战斗结束条件</summary>
        private void CheckEndConditions()
        {
            // 条件1：回合上限
            if (CurrentTurn >= MaxTurns)
            {
                EndCombat(CombatEndReason.TurnLimitReached);
                return;
            }

            // 条件2：场上只剩一方存活
            var activeTeams = _combatants
                .Where(c => c.IsActive)
                .Select(c => c.Team)
                .Distinct()
                .ToList();

            if (activeTeams.Count <= 1)
                EndCombat(CombatEndReason.AllDefeated);
        }

        /// <summary>执行战斗结束结算，生成 CombatResult</summary>
        private void EndCombat(CombatEndReason reason)
        {
            _phase = CombatPhase.Finished;

            var survivors    = _combatants.Where(c => c.IsActive).ToList();
            CombatTeam winner = CombatTeam.None;
            if (survivors.Count > 0)
            {
                var winnerTeams = survivors.Select(c => c.Team).Distinct().ToList();
                if (winnerTeams.Count == 1)
                    winner = winnerTeams[0];
            }

            var combatantResults = _combatants.Select(c => new CombatantResult
            {
                NpcId       = c.Id,
                Team        = c.Team,
                FinalStatus = c.Status,
                HpLost      = c.HpAtCombatStart - c.CurrentHp,
                TurnsActed  = c.TurnsActed,
            }).ToList();

            _result = new CombatResult
            {
                EndReason  = reason,
                WinnerTeam = winner,
                TotalTurns = CurrentTurn,
                Combatants = combatantResults,
            };

            Log($"\n战斗结束！原因={reason}，胜者={winner}，共{CurrentTurn}回合。");
        }

        /// <summary>校验当前阶段，不符则抛出异常</summary>
        private void AssertPhase(CombatPhase expected, string methodName)
        {
            if (_phase != expected)
                throw new InvalidOperationException(
                    $"CombatScene.{methodName} 需要阶段 [{expected}]，当前为 [{_phase}]。");
        }

        private static void Log(string msg) => Console.WriteLine($"[CombatScene] {msg}");

        // ── 内部状态机枚举 ────────────────────────────────────
        private enum CombatPhase
        {
            Idle,
            Initialized,
            PreStarted,
            Running,
            Finished,
        }
    }
}