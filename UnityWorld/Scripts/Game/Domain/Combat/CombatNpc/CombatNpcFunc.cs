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
        /// 应用治疗，不超过初始HP上限。
        /// </summary>
        public void ApplyHeal(int amount)
        {
            var val = Math.Min(amount, GetHpMax() - Hp);
            Scene.TriggerCombatEvent("OnHeal", new APIContext { Caster = this, Scene = Scene });
            RecoverHP(amount);
        }

        public void RecoverHP(int amount)
        {
            if(amount <= 0) return;
            Hp += amount;
        }
        public void CheckDefeated()
        {
            int sp = GetSp();
            int spMax = GetSpMax();
            if (sp > spMax && Status == CombatantStatus.Active)
            {
                Status = CombatantStatus.Defeated;
                PendingSlot = null;
                Log($"当前SP={sp}/{spMax}，状态：{Status}");
            } 
        }

        public void InitDeck()
        {
            var fieldIds = _loadoutSnapshot.Field;
            var reserveIds = _loadoutSnapshot.Reserve;
            var allCards = _loadoutSnapshot.AllCards;
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

        private ContestData TakePending()
        {
            var data = PendingSlot;
            PendingSlot = null;
            return data;
        }
        /// <summary>
        /// 直击处理：攻击直击=全额伤害，防御直击=无事发生。
        /// </summary>
        public CombatResult Straight(ContestData contestData)
        {
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
                ctx.Damage = contestData.ContestValue;
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
        }
        public void AddDamage(DamageInfo dmg)
        {
            if (dmg.SourceNpc != null)
            {
                var ctx = new APIContext { Caster = dmg.SourceNpc, SourceCard = dmg.SourceCard, Scene = Scene };
                ctx.Set("DamageInfo", dmg);
                dmg.SourceNpc.ModifyHook("DamageOut", ctx);
            }
            damageInfos.Enqueue(dmg);
        }
        /// <summary>
        /// 对拼结算：同类非射击攻击胜出取全额，其他攻击胜出取差值。广播 OnContestOverflow。
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

            int valueA = contestA.ContestValue;
            int valueB = contestB.ContestValue;

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

            int overflow = Math.Abs(valueA - valueB);
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

            contestA.SourceCard.Apply();
            contestB.SourceCard.Apply();

            return ret;
        }

        private CombatResult ContestWin(CombatResult ctx, ContestData win, ContestData lose, int overflow)
        {
            if (win.IsAttackType)
            {
                var dmg = new DamageInfo(win);
                dmg.TargetNpc = lose.OwnerNpc;
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
               func = c => c.Keywords.Contains("Wound") && c.Size == 2 && c.Shape.Count == c.Size;
            }
            else
            {
               func = c => c.Keywords.Contains("Wound") && c.Size == 1 && c.Shape.Count == c.Size;
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
