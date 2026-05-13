using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatNpc
    {
        
        
        public List<CombatCard> GetCards(CombatCardPhase phase)
        {
            return CardDeck.Where(c => c.GetPhase() == phase).ToList();
        }
        

        public void ApplyTrigger()
        {
            
        }

        /// <summary>
        /// 应用治疗，不超过初始HP上限。
        /// </summary>
        public void ApplyHeal(float amount)
        {
            
        }
        public void CheckDefeated()
        {
            int sp = GetSp();
            int spMax = GetSpMax();
            if (sp > spMax) Status = CombatantStatus.Defeated;
        }

        public void InitDeck()
        {
            var cardDeck = Owner.GetAllCards();
            if (cardDeck != null && cardDeck.Count > 0)
            {
                foreach (var card in cardDeck)
                {
                    var combatCard = CombatCard.CreateFromData(card);
                    combatCard.Owner = this;
                    CardDeck.Add(combatCard);
                }
            }                                    
            Log($"初始化卡组，卡牌数量={CardDeck.Count}, 当前SP={GetSp()}");        
        }

        public void ProcessContest()
        {
            var target = Target;
            while (PendingSlot.Count > Stats.Get("PendingSlotMax"))
            {
                if(target.PendingSlot.Count > 0)
                {
                    var Contest = PendingSlot.Dequeue();
                    var targetContest = target.PendingSlot.Dequeue();
                    //拼点
                    ResolveContest(Contest, targetContest);
                }
                else if (Ticks["Straight"] >= Stats.Get("StraightCD")*10)
                {
                    var Contest = PendingSlot.Dequeue();
                    Straight(Contest);
                }
                else break;
            }
        }
        /// <summary>
        /// 直击处理：接受 ContestData 参数。
        /// </summary>
        /// <returns>直击结果（包含伤害、是否 HP 清零等）</returns>
        public CombatResult Straight( ContestData contestData)
        {
            var attacker = contestData.OwnerNpc;
            var target = attacker.Target;
            var ret = new CombatResult();
            // ── Shield 溢出：叠甲到自身，不造成伤害，不触发攻击事件 ──────
            if (contestData.ContestType == ContestType.Shield)
            {
                float shieldGain = contestData.ContestValue;
                attacker.ChangeShield(shieldGain);
                Log($"  溢出: {contestData} 叠甲+{shieldGain:F0}，当前Shield={attacker.ShieldValue:F0}");
            }

            // ── Block 溢出：防值消失，不造成伤害，不触发攻击事件 ─────────
            if (contestData.ContestType == ContestType.Block)
            {
                Log($"  溢出: {contestData} 防值溢出消失");
            }
            if (contestData.IsAttackType)
            {
            Log($"  直击: {contestData} → [{target.GetName()}]");
            var ctx = new DamageInfo(contestData);
            ctx.Damage = contestData.ContestValue;
            ctx.TargetNpc.AddDamage(ctx);
            
            }
            // 重置来源卡 CD
            contestData.SourceCard.OnApply();
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
        /// 对拼结算：从双方 PendingSlot（ContestData）读取拼点数值。
        /// </summary>
        /// <returns>对拼结果（包含胜负、伤害、是否 HP 清零等）</returns>
        public CombatResult ResolveContest(ContestData contestA, ContestData contestB)
        {
            var ret = new CombatResult();
            var npcA = contestA.OwnerNpc;
            var npcB = contestB.OwnerNpc;

            // 安全检查：若任一方待发槽已空（上一轮对拼清掉），跳过
            if (contestA == null || contestB == null)
            {
                Log($"  对拼跳过: [{npcA.GetName()}]槽={contestA != null} [{npcB.GetName()}]槽={contestB != null}");
                ret.Set("IsEmpty", true);
                return ret;
            }
            Log($"  对拼: [{npcA.GetName()}]{contestA} vs [{npcB.GetName()}]{contestB}");

            float valueA = contestA.ContestValue;
            float valueB = contestB.ContestValue;
            float delta = Math.Abs(valueA - valueB);

            // 判断胜负
            if (Math.Abs(valueA - valueB) < 0.001f)
            {
                Log($"    平局，差值=0，无伤害");
                // 清空双方待发槽，重置 CD
                ret.Set("IsDraw", true);
                return ret;
            }

            CombatNpc winner, loser;
            ContestData winnerContest, loserContest;
            if (valueA > valueB)
            {
                winner = npcA; loser = npcB;
                winnerContest = contestA; loserContest = contestB;
            }
            else
            {
                winner = npcB; loser = npcA;
                winnerContest = contestB; loserContest = contestA;
            }
            winner.ContestWin(ret,winnerContest, loserContest);
            loser.ContestLose(ret,winnerContest, loserContest);
            
            Ticks["Straight"] = 0;
            
            contestA.SourceCard.OnApply();
            contestB.SourceCard.OnApply();
            
            return ret;
        }

        private CombatResult ContestWin(CombatResult ctx,ContestData win,ContestData lose)
        {
            
            if (win.IsAttackType)
            {
                // 构建伤害上下文
                var dmg = new DamageInfo(win);
                dmg.Damage = 0f;
                if(win.ContestType == lose.ContestType && lose.ContestType != ContestType.SheJi)
                {
                    //通吃
                    dmg.Damage = win.ContestValue;
                }
                else
                {
                    //差值
                    dmg.Damage = win.ContestValue - lose.ContestValue;
                }
                //加入伤害结算列表
                dmg.TargetNpc.AddDamage(dmg);
            }
            else if (win.ContestType == ContestType.Shield)
            {
                var val = win.ContestValue - lose.ContestValue;
                ChangeShield(val);
                Log($"盾卡胜，护盾+{val}，当前Shield={ShieldValue}");

                //差值
            }
            else if (win.ContestType == ContestType.Block)
            {
                //格挡
                Log($"防卡胜，差值消失");
            }
            
            // ── 赢家触发 trigger_on_attack ────────────────────────

            // ── 触发 trigger_on_contest_win / lose ────────────
            // ── 赢家通吃额外触发 Straight  ──────────
            return ctx;
        }

        
        private CombatResult ContestLose(CombatResult ctx,ContestData win,ContestData lose)
        {
            if (lose.IsAttackType)
            {
                if(lose.ContestType == lose.ContestType && lose.ContestType != ContestType.SheJi)
                {
                    //通吃
                }
                else
                {
                    //差值
                }
            }
            else if (lose.ContestType == ContestType.Shield)
            {
                //差值
            }
            else if (lose.ContestType == ContestType.Block)
            {
                //格挡
            }
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
                return woundCards[Soul.Random(0,woundCards.Count)].ID;
            }
            
            
            // 兜底
            return "card_wound_slash";
            
        }

       
    }
}