using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>一场独立的战斗。调用方可以逐 Tick 推进，也可以用 Run 一次运行到结算。</summary>
    public partial class CombatScene : GameEntityBase, ISoulBase
    {
        /// <summary>战斗当前的生命周期阶段</summary>
        private CombatPhase _phase = CombatPhase.Idle;

        /// <summary>本场战斗的随机数数据。</summary>
        public SoulData Soul { get; private set; }

        /// <summary>本场战斗允许推进的最大 Tick 数。</summary>
        public int MaxTicks { get; private set; } = 100;

        /// <summary>本场战斗已经推进的 Tick 数。</summary>
        public int CurrentTick { get; private set; }

        /// <summary>背包扫描一行所需的进度，普通速度每 Tick 增加一点。</summary>
        public int TicksPerRow { get; init; } = 10;

        /// <summary>本场战斗是否已结束。</summary>
        public bool IsFinished => _phase == CombatPhase.Finished;

        /// <summary>从大世界 NPC 创建本场参战者，并设置随机种子和 Tick 上限。</summary>
        public void Init(int guid, IEnumerable<(Npc npc, CombatTeam team)> npcParticipants, int maxTicks = 100)
        {
            AssertPhase(CombatPhase.Idle, nameof(Init));
            MaxTicks = maxTicks;
            if (maxTicks < 1) throw new ArgumentOutOfRangeException(nameof(maxTicks));
            if (TicksPerRow < 1) throw new ArgumentOutOfRangeException(nameof(TicksPerRow));

            InitCombatants(npcParticipants);
            Soul = new SoulData(guid);
            _phase = CombatPhase.Initialized;
        }

        /// <summary>准备各参战者的战斗数据，再分配初始目标。</summary>
        public void PreStart()
        {
            AssertPhase(CombatPhase.Initialized, nameof(PreStart));
            foreach (var npc in OrderedCombatants)
            {
                if (npc.Owner != null) npc.PreStart();
            }

            AssignTargets();
            _phase = CombatPhase.PreStarted;
            Log("PreStart完成，Target已确定。");
        }

        /// <summary>启动全部参战者，再让仍在场的角色进入各自的第一轮。</summary>
        public void Start()
        {
            AssertPhase(CombatPhase.PreStarted, nameof(Start));
            foreach (var npc in OrderedCombatants) npc.Start();

            _phase = CombatPhase.Running;
            Log("战斗开始！");
            foreach (var npc in OrderedCombatants)
            {
                npc.CheckDefeated();
                if (npc.IsActive) npc.BeginBackpackRound(opening: true);
            }
            FlushCombatChanges();
        }

        /// <summary>推进一个 Tick，处理到行的背包、战斗结束条件和存活角色的刷新。</summary>
        public void Tick()
        {
            mainScene = this;
            haslog = false;
            AssertPhase(CombatPhase.Running, nameof(Tick));
            CurrentTick++;
            foreach (var npc in OrderedCombatants) npc.DestroyMarkedCards();

            // 先让所有人按 Tick 开始时的速度累积进度。回调里新增的速度效果从下个 Tick 生效。
            foreach (var npc in ActiveCombatants) npc.Backpack.AdvanceNaturalTick();
            foreach (var npc in ActiveCombatants) npc.Tick();
            FlushCombatChanges();
            AssignTargets();

            ScanReadyRows();

            AssignTargets();
            if (!IsFinished) CheckEndConditions();
            foreach (var npc in ActiveCombatants) npc.Refresh();
        }

        /// <summary>从已初始化的场景运行到结算，并在退出时清理本场资源。</summary>
        public CombatResult Run()
        {
            if (openLog) mainScene = this;
            try
            {
                PreStart();
                Start();
                while (!IsFinished) Tick();
                return GetResult();
            }
            finally
            {
                Cleanup();
            }
        }

        /// <summary>先检查 Tick 上限，再检查是否只剩一个存活阵营。</summary>
        public void CheckEndConditions()
        {
            if (IsFinished) return;
            if (CurrentTick >= MaxTicks)
                EndCombat(CombatEndReason.TurnLimitReached);
            else if (HasAtMostOneActiveTeam())
                EndCombat(CombatEndReason.AllDefeated);
        }

        /// <summary>标记战斗结束，完成角色收尾并生成结果</summary>
        private void EndCombat(CombatEndReason reason)
        {
            SetPhaseFinished();
            foreach (var npc in OrderedCombatants) npc.End();
            foreach (var npc in OrderedCombatants) npc.DestroyMarkedCards();
            BuildCombatResult(reason);
        }

        /// <summary>清理参战者的监听、卡牌资源及本场尚未消费的表现记录。</summary>
        public void Cleanup()
        {
            foreach (var npc in OrderedCombatants) npc.Cleanup();
            ClearPresentationEvents();
            _queuedPresentationOrigins.Clear();
            if (ReferenceEquals(mainScene, this)) mainScene = null;
        }

        /// <summary>将战斗阶段设为已结束</summary>
        internal void SetPhaseFinished() => _phase = CombatPhase.Finished;

        /// <summary>断言当前阶段符合调用要求，否则抛出异常</summary>
        private void AssertPhase(CombatPhase expected, string methodName)
        {
            if (_phase != expected)
                throw new InvalidOperationException($"CombatScene.{methodName} 需要阶段 [{expected}]，当前为 [{_phase}]。");
        }
    }
}
