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
            if(target != null && target.IsActive)
            {
                Target = target;
                Log($"目标变更：{target.GetName()}");
            }
        }
        

        public void ApplyTrigger()
        {
            
        }

        /// <summary>
        /// 应用治疗，不超过初始HP上限。
        /// </summary>
        public void ApplyHeal(float amount)
        {
            var val = Math.Min(amount, GetHpMax() - Hp);
            EventMgr.Instance.TriggerEvent("OnHeal", val, (Scope.CombatNpc, Id.ToString()));
            Hp += val;
        }
        public void CheckDefeated()
        {
            int sp = GetSp();
            int spMax = GetSpMax();
            if (sp > spMax && Status == CombatantStatus.Active)
            {
                Status = CombatantStatus.Defeated;
                Log($"当前SP={sp}/{spMax}，状态：{Status}");
            } 
        }

        public void InitDeck()
        {
            var fieldIds = Owner.GetFieldIds();
            var reserveIds = Owner.GetReserveIds();
            var allCards = Owner.GetAllCards();

            // Field → Field（运转池）
            foreach (var cardId in fieldIds)
            {
                var card = allCards.Find(c => c.Id == cardId);
                if (card == null) continue;
                var combatCard = CombatCard.CreateFromData(card);
                combatCard.Owner = this;
                Field.Add(combatCard);
            }

            // Reserve → Reserve 池（候补）
            foreach (var cardId in reserveIds)
            {
                var card = allCards.Find(c => c.Id == cardId);
                if (card == null) continue;
                var combatCard = CombatCard.CreateFromData(card);
                combatCard.Owner = this;
                Reserve.Add(combatCard);
            }

            Log($"初始化卡组，运转池={Field.ToInfoString()}, Reserve={Reserve.Count}张, 当前SP={GetSp()}");
        }

        public void ProcessContest()
        {
            var target = Target;
            while (PendingSlot.Count > GetStat("PendingSlotMax"))
            {
                if(target.PendingSlot.Count > 0)
                {
                    var Contest = PendingSlot.Dequeue();
                    var targetContest = target.PendingSlot.Dequeue();
                    //拼点
                    ResolveContest(Contest, targetContest);
                }
                else if (Ticks["Straight"] >= GetStat("StraightCD")*10)
                {
                    var Contest = PendingSlot.Dequeue();
                    Straight(Contest);
                    
                }
                else break;
            }
        }
        /// <summary>
        /// 直击处理：攻击直击=全额伤害，防御直击=无事发生。
        /// </summary>
        public CombatResult Straight(ContestData contestData)
        {
            var attacker = contestData.OwnerNpc;
            var target = attacker.Target;
            var ret = new CombatResult();

                Scene.TriggerCombatEvent("OnStraight", new APIContext { Caster = this, SourceCard = contestData.SourceCard, Scene = Scene });
            if (contestData.IsAttackType)
            {
                Log($"  直击: {contestData} → [{target.GetName()}]");
                Scene.TriggerCombatEvent("OnAttack", new APIContext { Caster = this, SourceCard = contestData.SourceCard, Scene = Scene });
                var ctx = new DamageInfo(contestData);
                ctx.Damage = contestData.ContestValue;
                ctx.TargetNpc.AddDamage(ctx);
            }
            else
            {
                // 防御直击=无事发生，纯浪费
                Log($"  直击: {contestData} 防御溢出，无事发生");
            }

            contestData.SourceCard.Apply();
            Ticks["Straight"] = 0;
            return ret;
        }


        public void AddContestData(ContestData data)
        {
            PendingSlot.Enqueue(data);
        }
        public void AddDamage(DamageInfo dmg)
        {
            damageInfos.Enqueue(dmg);
        }
        /// <summary>
        /// 对拼结算：统一差值制。攻击赢→差值伤害，防御赢→无事。广播 OnContestOverflow。
        /// </summary>
        public CombatResult ResolveContest(ContestData contestA, ContestData contestB)
        {
            var ret = new CombatResult();
            var npcA = contestA.OwnerNpc;
            var npcB = contestB.OwnerNpc;

            if (contestA == null || contestB == null)
            {
                Log($"  对拼跳过: [{npcA.GetName()}]槽={contestA != null} [{npcB.GetName()}]槽={contestB != null}");
                ret.Set("IsEmpty", true);
                return ret;
            }

            float valueA = contestA.ContestValue;
            float valueB = contestB.ContestValue;

            Log($"  对拼: [{npcA.GetName()}]{contestA} vs [{npcB.GetName()}]{contestB}");

            // 平局
            if (Math.Abs(valueA - valueB) < 0.001f)
            {
                Log($"    平局，差值=0，无伤害");
                ret.Set("IsDraw", true);
                contestA.SourceCard.Apply();
                contestB.SourceCard.Apply();
                return ret;
            }

            // 判胜负
            ContestData winnerContest, loserContest;
            if (valueA > valueB)
            {
                winnerContest = contestA;
                loserContest = contestB;
            }
            else
            {
                winnerContest = contestB;
                loserContest = contestA;
            }

            float overflow = Math.Abs(valueA - valueB);
            var winner = winnerContest.OwnerNpc;
            var loser = loserContest.OwnerNpc;

            // 赢家处理
            winner.ContestWin(ret, winnerContest, loserContest, overflow);
            // 输家处理
            loser.ContestLose(ret, winnerContest, loserContest, overflow);

            foreach (var contest in new[] { winnerContest, loserContest })
            {
                var eventName = contest.IsAttackType ? "OnAttack" : contest.IsDefenseType ? "OnDefend" : null;
                if (eventName != null)
                    Scene.TriggerCombatEvent(eventName, new APIContext { Caster = contest.OwnerNpc, SourceCard = contest.SourceCard, Scene = Scene });
            }

            // 广播 OnContestOverflow
            var ctx = new APIContext
            {
                Caster = winner,
                SourceCard = winnerContest.SourceCard,
                Scene = Scene
            };
            ctx.Set("Winner", winner);
            ctx.Set("Loser", loser);
            ctx.Set("Overflow", overflow);
            ctx.Set("WinnerType", winnerContest.ContestType);
            ctx.Set("LoserType", loserContest.ContestType);
            ctx.Set("WinnerCard", winnerContest.SourceCard);
            ctx.Set("LoserCard", loserContest.SourceCard);
            Scene.TriggerCombatEvent("OnContestOverflow", ctx);

            Ticks["Straight"] = 0;
            contestA.SourceCard.Apply();
            contestB.SourceCard.Apply();

            return ret;
        }

        private CombatResult ContestWin(CombatResult ctx, ContestData win, ContestData lose, float overflow)
        {
            if (win.IsAttackType)
            {
                var dmg = new DamageInfo(win);
                // 同类型通吃：赢家全额伤害；异类型：差值伤害
                if (win.ContestType == lose.ContestType && lose.ContestType != ContestType.SheJi)
                    dmg.Damage = win.ContestValue;
                else
                    dmg.Damage = overflow;
                dmg.TargetNpc.AddDamage(dmg);
                Log($"  攻击胜，伤害={dmg.Damage:F0} → [{dmg.TargetNpc.GetName()}]");
            }
            // 防御赢：基础层无事，交给 OnContestOverflow 事件
            return ctx;
        }

        private CombatResult ContestLose(CombatResult ctx, ContestData win, ContestData lose, float overflow)
        {
            // 基础层：输了无额外惩罚，交给 OnContestOverflow 事件处理
            return ctx;
        }

        /// <summary>
        /// HP 清零处理：根据伤害来源选择对应伤势卡，塞入卡组，恢复 50% HP。
        /// </summary>
        public void HandleHpZero( DamageInfo ctx)
        {
            // 根据伤害来源的元素/物理类型选择对应的伤势卡 ID
            string woundCardId = ResolveWoundCardId(ctx);
            AddWound(woundCardId);
            // HP 恢复 50%
            Hp = GetCombatHpMax() * 0.5f;
         }
        
        public CombatCard AddWound(string woundCardId)
        {
            // 通过 CardMgr 从 Define 实例化完整的 CardData（含 Effect/Action）
            var injuryCard = CardMgr.Instance?.InstantiateFromDefine(woundCardId);
            if (injuryCard == null)
            {
                // 兜底：如果 Define 缺失，用最基础的伤口
                Log($"  [警告] 找不到伤势卡 Define：{woundCardId}，使用默认 card_wound_slash");
                injuryCard = CardMgr.Instance?.InstantiateFromDefine("card_wound_slash");
            }
            // 伤势卡塞入卡组
            var card = CombatCard.CreateFromData(injuryCard);
            AddCombatCard(card);
            return card;
        }
        /// <summary>
        /// 根据 DamageInfo 的元素和物理类型，选择对应的伤势卡 DefineId。
        /// 优先级：有元素伤害时选元素伤势卡，否则按物理类型选；
        /// 重伤（FinalDamage > 25）覆盖为 card_wound_severe。
        /// </summary>
        private string ResolveWoundCardId(DamageInfo ctx)
        {
            // 重伤判定：伤害值超过阈值时固定生成重伤卡
            Func<CardDefine, bool> func = c => true;
            if(ctx.Damage > GetTiPo())
            {
               func = c => c.Keywords.Contains("Wound") && c.Size == 2;
            }
            else
            {
               func = c => c.Keywords.Contains("Wound") && c.Size == 1;
            }
            var woundCards = CardDefineMgr.Instance?.Query(func).ToList();
            if(woundCards.Count > 0)
            {
                return woundCards[Scene.Soul.Random(0,woundCards.Count)].ID;
            }
            
            
            // 兜底
            return "card_wound_slash";
            
        }

       
    }
}