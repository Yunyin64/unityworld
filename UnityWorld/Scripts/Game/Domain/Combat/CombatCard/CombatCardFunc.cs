using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatCard
    {
        // ── 拼点与待发槽 ──

        /// <summary>是否通过 Contest 回调创建待发拼点。</summary>
        public bool ProducesContest => BaseData.ProducesContest ?? this.HasHook("Contest");

        /// <summary>
        /// 尝试将卡推入待发槽。
        /// 构造 ContestData 快照后放入 PendingSlot。
        /// </summary>
        /// <param name="contestType">拼点类型（Zhan/Ci/Da/SheJi/Shield/Block）</param>
        /// <param name="element">元素类型（仅攻击类有效）</param>
        /// <param name="contestValue">拼点数值（攻击值/盾值/防值）</param>
        public void TryPushToPendingSlot(ContestType contestType, ElementType element, int contestValue)
        {
            // 只有当次已授权的法术或招式 Contest 回调能生成待发数据。
            if (Phase != CombatCardPhase.Active || !AwaitingApply || Scene?.IsExecutingActivation(this) != true)
                throw new InvalidOperationException($"{DefineId} 只能在获准的拼点回调中入槽。");
            if (Owner.PendingSlot != null) throw new InvalidOperationException("唯一待发槽已占用。");
            // 构造 ContestData 快照
            var contestData = new ContestData
            {
                ContestType = contestType,
                Element = element,
                ContestValue = contestValue,
                SourceCard = this,
                PresentationActionId = PresentationActionId
            };
            // 修正器可以读取卡牌阶段，因此先标记待发，再修正点数并入槽。
            SetPhase(CombatCardPhase.InPending);

            Owner.ModifyHook("Contest", CreateCtx("ContestData", contestData));
            Owner.AddContestData(contestData);

            Log($"  入槽: {contestData}  [{Owner.GetName()}] ");
        }

        internal void ModifyPairedContest(ContestData contest)
        {
            if (!this.HasHook("OnBeforePairedContest") || CallMainKeyword("CanRespond", argument: true) is not true) return;
            var ctx = new APIContext { Caster = contest.OwnerNpc, Scene = Scene, SourceCard = contest.SourceCard };
            ctx.Set("ContestData", contest);
            var previous = contest.ContestValue;
            using var presentation = Scene.BeginPresentationEffect(this, "OnBeforePairedContest");
            this.CallLuaHook<bool>("OnBeforePairedContest", env, ctx);
            Scene.ThrowIfEventAborted();
            if (contest.ContestValue != previous) Scene.MarkPresentationEffect();
            presentation?.Complete();
        }

        // ── 冷却与充能 ──

        /// <summary>保留旧 CD 时钟入口，主动发动由背包扫描驱动。</summary>
        public void CDTick()
        {
        }

        private float GetCDAdd()
        {
            var speed = GetStat("CDSpeed");
            var increment = 1f;
            if (speed > 0) increment *= 1 + speed / 10;
            if (speed < 0) increment /= 1 + (-speed) / 10f;
            return increment <= 0.1f ? 0 : increment;
        }

        /// <summary>读取含战斗修正的冷却上限。</summary>
        public float GetCDMax()
        {
            return GetCooldown() + GetStat("CDTickAdj");
        }

        /// <summary>按当前CDSpeed计算还需多少真实Tick才能CD满，返回-1表示无法完成</summary>
        public int GetCDTickRemaining()
        {
            var increment = GetCDAdd();
            if (increment <= 0.1f) return -1;
            var remaining = GetCDMax() - Ticks["CD"];
            if (remaining <= 0) return 0;
            return (int)Math.Ceiling(remaining / increment);
        }

        /// <summary>将旧 CD 计时值重置为 1。</summary>
        public void ResetCD()
        {
            Ticks["CD"] = 1;
        }

        /// <summary>输出当前 CD 与上限。</summary>
        public void LogCDInfo()
        {
            Log($" CD: {Ticks["CD"]} | {GetCDMax()}  ");
        }

        /// <summary>为持有者的背包扫描充能。</summary>
        public void Charge(int Tick)
        {
            Owner.Backpack.Charge(Tick);
            Log($"背包充能基准进度 +{Math.Max(0, Tick)}");
        }

        /// <summary>向本场战斗写入带卡牌标识的日志。</summary>
        public void Log(string msg)
        {
            if (Owner?.Scene != null) Owner.Scene.Trace(Owner, $"[Card|{DefineId}] {msg}");
            else CombatScene.Log($"[Card|{DisplayName}] {msg}");
        }
    }
}
