using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatNpc
    {
        public List<CombatCard> GetCards(CombatCardPhase phase)
        {
            return Field.Where(c => c.CheckPhase(phase)).ToList();
        }

        public CombatNpc GetTarget()
        {
            return Target;
        }
        public void ChangeTarget(CombatNpc target)
        {
            if (target == null) { Target = null; return; }
            if(target.IsActive)
            {
                Target = target;
                Log($"目标变更：{target.GetName()}");
            }
        }
        

        public void ApplyTrigger()
        {
            
        }

        /// <summary>
        /// 应用无指定施术者的治疗，只结算目标的受疗修正，实际回复量受最大 HP 限制。
        /// </summary>
        public void ApplyHeal(int amount) => ApplyHeal(amount, null);

        /// <summary>先结算施术者的治疗量，再结算目标的受疗量，最后按目标缺失的 HP 截断。</summary>
        internal void ApplyHeal(int amount, CombatNpc source)
        {
            // 目标满血或基础治疗量非正时，保留双方的单次状态。
            if (amount <= 0 || Hp >= GetCombatHpMax()) return;

            var ctx = new APIContext { Caster = source, Scene = Scene, NpcTargets = [this] };
            if (source != null)
                amount = source.ModifyAmount("HealOut", amount, ctx);
            // 出手治疗量归零时，保留目标的单次受疗状态。
            if (amount <= 0) return;

            amount = ModifyAmount("HealIn", amount, ctx);
            var actual = Math.Min(amount, GetCombatHpMax() - Hp);
            if (actual <= 0) return;

            // 治疗监听读取结算后的 HP。满血和无效治疗不产生事件。
            RecoverHP(actual);
            var healed = new APIContext { Caster = this, Scene = Scene };
            healed.Set("Amount", actual);
            healed.Set("SourceNpc", source);
            Scene.TriggerCombatEvent("OnHeal", healed);
        }

        public void RecoverHP(int amount)
        {
            if(amount <= 0) return;
            Hp += amount;
            Scene?.MarkPresentationEffect();
        }
        public void CheckDefeated()
        {
            int sp = GetSp();
            int spMax = GetSpMax();
            if (sp > spMax && Status == CombatantStatus.Active)
            {
                Status = CombatantStatus.Defeated;
                CancelPending("Defeated");
                Log($"当前SP={sp}/{spMax}，状态：{Status}");
            } 
        }

        public void InitDeck()
        {
            var fieldIds = CardData.Field;
            var reserveIds = CardData.Reserve;
            var allCards = CardData.AllCards;
            if (Field.Count > 0 || Reserve.Count > 0)
                throw new InvalidOperationException("战斗卡组不能重复初始化。");

            // Field → Field（运转池）
            foreach (var cardId in fieldIds)
            {
                var card = allCards.Find(c => c.Id == cardId);
                if (card == null) continue;
                var combatCard = (CombatCard)card;
                combatCard.Owner = this;
                Field.Add(combatCard);
            }

            // Reserve → Reserve 池（候补）
            foreach (var cardId in reserveIds)
            {
                var card = allCards.Find(c => c.Id == cardId);
                if (card == null) continue;
                var combatCard = (CombatCard)card;
                combatCard.Owner = this;
                Reserve.Add(combatCard);
            }

            Log($"初始化卡组，运转池={Field.ToInfoString()}, Reserve={Reserve.Count}张, 当前SP={GetSp()}");
        }

        public void ProcessContest()
        {
            var target = Target;
            if (!IsActive || target == null || !target.IsActive || PendingSlot == null || target.PendingSlot == null) return;
            ProcessContestAtBoundary(PendingSlot, target, target.PendingSlot);
        }

        /// <summary>只结算本批行边界快照中的槽；结算事件新产生的槽留给后续边界。</summary>
        internal void ProcessContestAtBoundary(ContestData expectedOwn, CombatNpc target, ContestData expectedOther)
        {
            if (!Scene.AtContestBoundary || !IsActive || !target.IsActive || expectedOwn == null || expectedOther == null ||
                !ReferenceEquals(PendingSlot, expectedOwn) || !ReferenceEquals(target.PendingSlot, expectedOther)) return;
            var own = TakePending();
            var other = target.TakePending();
            Log($"行切换对拼 owner={Id} 对手={target.Id}");
            ResolveContest(own, other);
        }

        /// <summary>本方轮末只处理自己剩余的一份；空槽不会触发对手。</summary>
        internal void ForceOwnPending()
        {
            if (!IsActive || PendingSlot == null) return;
            Log($"轮末强制 owner={Id} 卡={PendingSlot.SourceCard.DefineId}");
            Straight(TakePending());
        }

        // 取消只结束待发，不执行 Apply。沿用本卡的 Lua 分派，让关键词处理收尾。
        private void CancelPending(string reason)
        {
            if (PendingSlot == null) return;
            var pending = PendingSlot;
            PendingSlot = null;
            RecordPending(pending, false, reason);
            var card = pending.SourceCard;
            if (card.CheckPhase(CombatCardPhase.InPending)) card.SetPhase(card.ChargePhase);
            var ctx = new APIContext { Scene = Scene, Caster = this, SourceCard = card };
            ctx.Set("Reason", reason);
            card.CallLua("OnPendingCancelled", ctx);
        }

        private ContestData TakePending()
        {
            var data = PendingSlot;
            PendingSlot = null;
            if (data != null) RecordPending(data, false, "Taken");
            return data;
        }
        private void RecordPending(ContestData data, bool entered, string reason) =>
            Scene.RecordPresentation(CombatPresentationKind.PendingChanged, this, data.SourceCard,
                new PresentationOrigin(data.PresentationActionId, null, data.PresentationContestId, null),
                pending: new PendingPresentation(entered, reason, data.ContestType.ToString(), data.Element.ToString(), data.FinalValue));
        /// <summary>
        /// 直击处理：攻击直击=全额伤害，防御直击=无事发生。
        /// </summary>
        public CombatResult Straight(ContestData contestData)
        {
            using var presentationOrigin = Scene.EnterPresentationOrigin(new PresentationOrigin(contestData.PresentationActionId, null, null, null));
            var attacker = contestData.OwnerNpc;
            var target = attacker.Target;
            var ret = new CombatResult();
            if (target == null || !target.IsActive) { contestData.SourceCard.Apply(); return ret; }

                Scene.TriggerCombatEvent("OnStraight", new APIContext { Caster = this, SourceCard = contestData.SourceCard, Scene = Scene });
            if (contestData.IsAttackType)
            {
                Log($"  直击: {contestData} → [{target.GetName()}]");
                Scene.TriggerCombatEvent("OnAttack", new APIContext { Caster = this, SourceCard = contestData.SourceCard, Scene = Scene });
                var ctx = new DamageInfo(contestData);
                ctx.Damage = contestData.FinalValue;
                ctx.TargetNpc.AddDamage(ctx);
            }
            else
            {
                // 防御直击=无事发生，纯浪费
                Log($"  直击: {contestData} 防御溢出，无事发生");
            }

            contestData.SourceCard.Apply();
            return ret;
        }


        public void AddContestData(ContestData data)
        {
            // Modifier 修正拼点数值;
            if (data.OwnerNpc != this) throw new InvalidOperationException("待发数据所属者不匹配。");
            if (PendingSlot != null) throw new InvalidOperationException("每方待发槽只有一个位置。");
            PendingSlot = data;
            RecordPending(data, true, "Entered");
        }
        public void AddDamage(DamageInfo dmg)
        {
            dmg.PresentationOrigin ??= Scene.CapturePresentationOrigin();
            if (dmg.SourceNpc != null && dmg.Damage > 0)
            {
                var ctx = new APIContext { Caster = dmg.SourceNpc, SourceCard = dmg.SourceCard, Scene = Scene };
                ctx.Set("DamageInfo", dmg);
                dmg.SourceNpc.ModifyHook("DamageOut", ctx);
            }
            dmg.Damage = Math.Clamp(dmg.Damage, 0, int.MaxValue);
            damageInfos.Enqueue(dmg);
            if (dmg.Damage > 0) Scene.MarkPresentationEffect();
        }
        /// <summary>
        /// 对拼结算：按差值生成基础伤害，由双方来源卡的关键词修正后提交。
        /// </summary>
        public CombatResult ResolveContest(ContestData contestA, ContestData contestB)
        {
            var ret = new CombatResult();
            if (contestA == null || contestB == null)
            {
                Log("  对拼跳过：双方待发数据未齐全");
                ret.Set("IsEmpty", true);
                return ret;
            }

            var npcA = contestA.OwnerNpc;
            var npcB = contestB.OwnerNpc;
            if (!Scene.AtContestBoundary)
                throw new InvalidOperationException("只能在战斗事件处理结束后的行边界进行配对。");

            var contestId = Scene.NextPresentationIdentity();
            contestA.PresentationContestId = contestB.PresentationContestId = contestId;
            contestA.SourceCard.PresentationContestId = contestB.SourceCard.PresentationContestId = contestId;
            using var presentationOrigin = Scene.EnterPresentationOrigin(new PresentationOrigin(null, null, contestId, null));
            // 双方到达实际配对边界后，先由各自主词处理该拼点，再执行角色修正。
            contestA.SourceCard.CallMainKeyword("OnBeforePairedContest", argument: contestA);
            contestB.SourceCard.CallMainKeyword("OnBeforePairedContest", argument: contestB);
            npcB.ApplyPairedContestModifiers(contestB, contestA);
            npcA.ApplyPairedContestModifiers(contestA, contestB);
            contestA.ContestValue = contestA.FinalValue;
            contestB.ContestValue = contestB.FinalValue;

            // 对拼事件报告本次配对的参与者、来源卡与类型。
            var pairedContext = new APIContext { Caster = npcA, SourceCard = contestA.SourceCard, Scene = Scene };
            pairedContext.Set("ContestA", npcA);
            pairedContext.Set("ContestB", npcB);
            pairedContext.Set("ContestCardA", contestA.SourceCard);
            pairedContext.Set("ContestCardB", contestB.SourceCard);
            pairedContext.Set("ContestTypeA", contestA.ContestType.ToString());
            pairedContext.Set("ContestTypeB", contestB.ContestType.ToString());
            Scene.TriggerCombatEvent("OnContest", pairedContext);
            int valueA = contestA.FinalValue;
            int valueB = contestB.FinalValue;

            // 用最终拼点判定对拼结果，表现和结算共用同一次比较。
            int comparison = valueA.CompareTo(valueB);
            bool isDraw = comparison == 0;
            int overflow = Math.Abs(valueA - valueB);
            if (Scene.PresentationRecordingEnabled)
            {
                ContestSidePresentation Side(ContestData data) => new(data.OwnerNpc.Id,
                    CombatScene.PresentationCard(data.SourceCard), data.PresentationActionId,
                    data.ContestType.ToString(), data.Element.ToString(), data.FinalValue);
                Scene.RecordPresentation(CombatPresentationKind.ContestResolved, npcA, contestA.SourceCard,
                    contest: new ContestPresentation(Side(contestA), Side(contestB),
                        isDraw ? null : comparison > 0 ? npcA.Id : npcB.Id, overflow));
            }

            Log($"  对拼: [{npcA.GetName()}]{contestA} vs [{npcB.GetName()}]{contestB}");

            pairedContext.Set("IsDraw", isDraw);
            pairedContext.Set("Overflow", overflow);
            pairedContext.Set("ContestValueA", valueA);
            pairedContext.Set("ContestValueB", valueB);
            if (isDraw)
            {
                Log("    平局，差值=0，无伤害");
                ret.Set("IsDraw", true);
            }
            else
            {
                var winnerContest = comparison > 0 ? contestA : contestB;
                var loserContest = comparison > 0 ? contestB : contestA;
                var winner = winnerContest.OwnerNpc;
                var loser = loserContest.OwnerNpc;

                // 有胜负时，事件主体和来源卡取胜方。
                pairedContext.Caster = winner;
                pairedContext.SourceCard = winnerContest.SourceCard;
                pairedContext.Set("Winner", winner);
                pairedContext.Set("Loser", loser);
                pairedContext.Set("WinnerType", winnerContest.ContestType);
                pairedContext.Set("WinnerTypeName", winnerContest.ContestType.ToString());
                pairedContext.Set("LoserType", loserContest.ContestType);
                pairedContext.Set("LoserTypeName", loserContest.ContestType.ToString());
                pairedContext.Set("WinnerValue", Math.Max(valueA, valueB));
                pairedContext.Set("LoserValue", Math.Min(valueA, valueB));
                pairedContext.Set("WinnerCard", winnerContest.SourceCard);
                pairedContext.Set("LoserCard", loserContest.SourceCard);
                ResolveContestKeywords(pairedContext, winnerContest, loserContest, overflow);

                foreach (var contest in new[] { winnerContest, loserContest })
                {
                    var eventName = contest.IsAttackType ? "OnAttack" : contest.IsDefenseType ? "OnDefend" : null;
                    if (eventName != null)
                        Scene.TriggerCombatEvent(eventName, new APIContext { Caster = contest.OwnerNpc, SourceCard = contest.SourceCard, Scene = Scene });
                }
            }

            Scene.TriggerCombatEvent("OnContestOverflow", pairedContext);
            contestA.SourceCard.Apply();
            contestB.SourceCard.Apply();

            return ret;
        }

        /// <summary>让本方场上卡牌对指定的实际拼点执行配对前修正。</summary>
        public void ApplyFieldContestModifiers(ContestData contest)
        {
            foreach (var card in Field) card.ModifyPairedContest(contest);
        }

        private void ApplyPairedContestModifiers(ContestData contest, ContestData opponentContest)
        {
            // 持有者的 Buff 在配对结算前可以读取本次实际配对的两份拼点。
            var ctx = new APIContext { Caster = this, SourceCard = contest.SourceCard, Scene = Scene };
            ctx.Set("ContestData", contest);
            ctx.Set("OpponentContestData", opponentContest);
            ModifyHook("PairedContest", ctx);
        }

        private void ResolveContestKeywords(APIContext pairedContext, ContestData winner, ContestData loser, int overflow)
        {
            var damage = winner.IsAttackType ? new DamageInfo(winner) { TargetNpc = loser.OwnerNpc } : null;
            long baseDamage = damage == null ? 0 : overflow;
            // 只同步交给本次的两张来源卡。双方分别获得自己的 Self 上下文，只传递伤害结果。
            foreach (var side in new[] { winner, loser })
            {
                var ctx = pairedContext.CopyForEvent();
                ctx.Caster = side.OwnerNpc;
                ctx.SourceCard = side.SourceCard;
                ctx.Set("IsWinner", ReferenceEquals(side, winner));
                ctx.Set("WinnerIsAttack", winner.IsAttackType);
                ctx.Set("LoserIsAttack", loser.IsAttackType);
                ctx.Set("IsEnemy", winner.OwnerNpc.Team != loser.OwnerNpc.Team);
                ctx.Set("BaseDamage", baseDamage);
                side.SourceCard.RunKeywordHooks("OnResolveContest", ctx);
                baseDamage = ctx.GetValue("BaseDamage", baseDamage);
            }

            if (damage == null) return;
            damage.Damage = Math.Max(0, baseDamage);
            damage.TargetNpc.AddDamage(damage);
            Log($"  攻击胜，伤害={damage.Damage:F0} → [{damage.TargetNpc.GetName()}]");
        }

        /// <summary>
        /// HP 清零处理：根据伤害来源选择对应伤势卡，塞入卡组，恢复 50% HP。
        /// </summary>
        public void HandleHpZero( DamageInfo ctx)
        {
            // 根据伤害来源的元素/物理类型选择对应的伤势卡 ID
            string woundCardId = ResolveWoundCardId(ctx);
            AddWound(woundCardId);
            Hp = (GetCombatHpMax() * GetStat("HpZeroRecoverRate")).ToInt() ;
         }
        
        public CombatCard AddWound(string woundCardId)
        {
            // 通过 CardMgr 从 Define 实例化完整的 CardData（含 Effect/Action）
            var injuryCard = CardMgr.Instance?.InstantiateFromDefine(woundCardId);
            if (injuryCard == null) throw new InvalidOperationException($"缺少完整伤口卡定义：{woundCardId}。");
            // 伤势卡塞入卡组
            var card = CombatCard.CreateFromData(injuryCard);
            AddCombatCard(card);
            return card;
        }
        /// <summary>
        /// 沿用伤害与体魄的轻重伤选择，只从具备完整形状的新伤口定义中抽取。
        /// </summary>
        private string ResolveWoundCardId(DamageInfo ctx)
        {
            // 重伤判定：伤害值超过阈值时固定生成重伤卡
            Func<CardDefine, bool> func = c => true;
            if(ctx.Damage > GetTiPo())
            {
               func = c => c.Keywords.Contains("Wound") && c.Size == 2 && c.GetShape().Count == c.Size && c.CompatibilityStatus == "Migrated";
            }
            else
            {
               func = c => c.Keywords.Contains("Wound") && c.Size == 1 && c.GetShape().Count == c.Size && c.CompatibilityStatus == "Migrated";
            }
            var woundCards = CardDefineMgr.Instance?.Query(func).ToList();
            if(woundCards != null && woundCards.Count > 0)
            {
                return woundCards[Scene.Soul.Random(0,woundCards.Count)].ID;
            }
            
            
            throw new InvalidOperationException("缺少符合轻重伤条件且具备完整形状的伤口卡定义。");
            
        }

       
    }
}
