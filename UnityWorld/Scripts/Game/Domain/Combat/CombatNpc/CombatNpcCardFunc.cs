using System.Collections;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatNpc
    {
        /// <summary>当前在背包运转的卡牌</summary>
        protected List<CombatCard> Field { get; set; } = new();

        /// <summary>候补池：静默，不Tick、不占SP</summary>
        protected List<CombatCard> Reserve { get; set; } = new();

        /// <summary>待处理的场上卡增删队列</summary>
        private readonly Queue<(CombatFieldChangeType Type, CombatCard Card,
            PresentationOrigin Origin, string RemovalReason)> _changes = new();

        /// <summary>返回本角色当前的场上卡列表</summary>
        public List<CombatCard> GetField()
        {
            return Field;
        }

        /// <summary>获取候补池</summary>
        public List<CombatCard> GetReserve()
        {
            return Reserve;
        }

        private readonly HashSet<int> _initialCards = new();

        /// <summary>从本场卡组中移除已登记销毁的实例。</summary>
        internal void DestroyMarkedCards()
        {
            var toRemove = CardData.AllCards.OfType<CombatCard>()
                .Where(card => card.MarkedForDestruction).ToArray();
            foreach (var card in toRemove)
            {
                // 战末仍在场的临时卡也要释放占格和监听。
                if (Field.Contains(card))
                {
                    Backpack.RemoveCard(card.Id);
                    Field.Remove(card);
                    card.Cleanup();
                }
                CardData.AllCards.Remove(card);
                CardData.AllCardIds.Remove(card.Id);
            }
        }

        /// <summary>从战末卡组汇总本场新增的普通伤口。</summary>
        internal void CollectCardSettlement(CombatantResult result)
        {
            result.GainedWounds = CardData.AllCards
                .Where(card => !_initialCards.Contains(card.Id) && card.HasKeyword("Wound") && !card.HasKeyword("Temp"))
                .Select(card => card.DefineId).ToArray();
        }

        /// <summary>汇总本场各消耗卡的初始及剩余次数</summary>
        internal IReadOnlyList<CombatConsumptionResult> GetConsumptionResults()
        {
            return Array.AsReadOnly(CardData.AllCards.OfType<CombatCard>().Where(card => _initialCards.Contains(card.Id) && card.InitialConsumeStack >= 0)
                .OrderBy(card => card.Id)
                .Select(card => new CombatConsumptionResult(card.Id, card.InitialConsumeStack, card.GetStack(), card.GetStack())).ToArray());
        }

        /// <summary>按场上列表下标取卡</summary>
        public CombatCard GetCardByIndex(int index)
        {
            return Field[index];
        }

        /// <summary>查找卡牌在场上列表中的下标</summary>
        public int GetIndexByCard(CombatCard card)
        {
            return Field.IndexOf(card);
        }

        /// <summary>将旧调用视为普通效果移除。相同卡的重复请求只会实际离场一次。</summary>
        public void RemoveCombatCard(CombatCard card) => QueueCardRemoval(card, "Effect");

        /// <summary>请求治愈场上的伤势，实际离场后才通知监听者。</summary>
        internal void HealWound(CombatCard card) => QueueCardRemoval(card, "WoundHealed");

        /// <summary>把带原因和回放来源的卡牌移除请求入队</summary>
        internal void QueueCardRemoval(CombatCard card, string reason)
        {
            _changes.Enqueue((CombatFieldChangeType.Remove, card, Scene.CapturePresentationOrigin(), reason));
            if (Field.Contains(card)) Scene.MarkPresentationEffect();
        }

        /// <summary>把卡牌入场请求连同回放来源入队</summary>
        public void AddCombatCard(CombatCard card)
        {
            _changes.Enqueue((CombatFieldChangeType.Add, card, Scene.CapturePresentationOrigin(), ""));
            if (Scene.PresentationRecordingEnabled && CanObserveAdd(card)) Scene.MarkPresentationEffect();
        }
        // 只做无副作用的请求有效性检查；验证失败仍由原 DealFieldChange 路径负责抛出。
        /// <summary>预查卡牌入场是否能找到可用位置</summary>
        private bool CanObserveAdd(CombatCard card)
        {
            try { return card != null && !Backpack.Placements.ContainsKey(card.Id) && Backpack.FindPlacement(card, card.HasKeyword("Wound")) != null; }
            catch { return false; }
        }
        
        /// <summary>立即滑动本角色背包中的卡牌，成功移动才返回 true。外部效果可指定发起者和来源卡。</summary>
        public bool SlideCombatCard(CombatCard card, CombatCardSlideType direction, CombatNpc mover = null, CombatCard sourceCard = null)
            => Backpack.TrySlide(card, direction, mover, sourceCard);

        /// <summary>部署：将卡从 Reserve 移入 Field（延迟执行）</summary>
        public void Deploy(CombatCard card)
        {
            throw new NotSupportedException("候补部署的背包摆放规则待卡牌设计迁移。");
        }

        /// <summary>召回：将卡从 Field 移入 Reserve（延迟执行）</summary>
        public void Recall(CombatCard card)
        {
            throw new NotSupportedException("召回效果待卡牌设计迁移。");
        }

        // ── 招式轮转 ──────────────────────────────────────────

        /// <summary>筛出本角色场上的全部招式卡</summary>
        public List<CombatCard> GetZhaoShiList()
        {
            return Field.Where(c => c.HasKeyword("ZhaoShi")).ToList();
        }

        /// <summary>检查招式实例的已装备父来源，父卡的摆放与激活状态不参与判断。</summary>
        public bool HasCombatFormSource(CombatCard card) => card.ParentCardId != card.Id &&
            CardData.EquippedFaBao.Contains(card.ParentCardId) &&
            _battleEquipment.TryGetValue(card.ParentCardId, out var equip) && equip.FormList.Contains(card.DefineId);

        /// <summary>读取招式轮转当前指向的卡牌编号</summary>
        public int GetCurrentZhaoShiCardId()
        {
            return CurrentZhaoShiCardId;
        }

        /// <summary>设置招式轮转当前指向的卡牌编号</summary>
        public void SetCurrentZhaoShiCardId(int id)
        {
            CurrentZhaoShiCardId = id;
        }



        /// <summary>将招式轮转指向首张招式卡，无招式时清空</summary>
        public void InitZhaoShiRotation()
        {
            var list = GetZhaoShiList();
            CurrentZhaoShiCardId = list.Count > 0 ? list[0].Id : -1;
        }

        // ── 统一处理队列 ──────────────────────────────────────────

        /// <summary>处理场上卡变化队列，同步背包并派发事件</summary>
        public void DealFieldChange()
        {
            while (_changes.Count > 0)
            {
                var (changeType, card, origin, removalReason) = _changes.Dequeue();
                using var presentationOrigin = Scene.EnterPresentationOrigin(origin);
                switch (changeType)
                {
                    case CombatFieldChangeType.Add:
                        if (card.MarkedForDestruction) break;
                        var manaBefore = Scene.PresentationRecordingEnabled ? Backpack.Mana.ToDictionary(p => p.Key, p => p.Value) : null;
                        var replaced = Backpack.Insert(card, card.HasKeyword("Wound"));
                        // 原背包在入场时创建空蓄灵；现在由战斗卡重置自己的状态。
                        card.ClearAssignedMana();
                        Field.Add(card);
                        card.Owner = this;
                        card.PreStart();
                        card.Start();
                        if (manaBefore != null)
                            Scene.RecordPresentation(CombatPresentationKind.CardAddedToBackpack, this, card, origin,
                                added: new CardAddedPresentation(CombatScene.PresentationCopy(replaced.Select(c =>
                                    new PresentationManaCell(new PresentationCell(c.X, c.Y), manaBefore[c].ToString())))));
                        foreach (var cell in replaced) Log($"伤口替代灵元 卡={card.DefineId} 格({cell.X + 1},{cell.Y + 1})");
                        Log($"[Field+] {card.DisplayName}，当前SP={GetSp()}");
                        if (card.HasKeyword("Wound"))
                        {
                            var gained = new APIContext { Caster = this, SourceCard = card, Scene = Scene };
                            gained.Set("WoundSize", card.GetSize());
                            Scene.TriggerCombatEvent("OnWoundGained", gained);
                        }
                        break;

                    case CombatFieldChangeType.Remove:
                        if (!Field.Contains(card) && !Reserve.Contains(card)) break;
                        var healed = removalReason == "WoundHealed" && Field.Contains(card) && card.HasKeyword("Wound");
                        if (Field.Contains(card)) Backpack.RemoveCard(card.Id);
                        Field.Remove(card);
                        Reserve.Remove(card);
                        CardData.Reserve.Remove(card.Id);
                        card.ClearAssignedMana();
                        var left = new APIContext { Caster = this, SourceCard = card, Scene = Scene };
                        // 只有实际生效的请求能说明离场原因。此时占格和在场 SP 已经释放。
                        left.Set("RemovalReason", removalReason);
                        card.InvokeOwnHook("OnLeave", left);
                        card.DispatchKeywordHooks("OnLeave", left, acceptDestruction: true);
                        card.End();
                        Scene.TriggerCombatEvent("OnCardLeave", left);
                        if (healed)
                        {
                            // 只销毁本场治愈的新伤，旧伤仍按原样留在世界角色手中。
                            if (!_initialCards.Contains(card.Id) && !card.HasKeyword("Temp"))
                                card.MarkedForDestruction = true;
                            left.Set("WoundSize", card.GetSize());
                            Scene.TriggerCombatEvent("OnWoundHealed", left);
                        }
                        Log($"[Field-] {card.DisplayName}，当前SP={GetSp()}");
                        break;

                    case CombatFieldChangeType.Deploy:
                        if (!Reserve.Contains(card))
                        {
                            Log($"[Deploy] 警告：{card.DisplayName} 不在 Reserve 中，跳过");
                            break;
                        }
                        Reserve.Remove(card);
                        Field.Add(card);
                        card.Start();
                        card.CallLua("OnDeploy");
                        Log($"[Deploy] {card.DisplayName} Reserve→Field，当前SP={GetSp()}");
                        break;

                    case CombatFieldChangeType.Recall:
                        if (!Field.Contains(card))
                        {
                            Log($"[Recall] 警告：{card.DisplayName} 不在 Field 中，跳过");
                            break;
                        }
                        Field.Remove(card);
                        Reserve.Add(card);
                        card.CallLua("OnRecall");
                        card.End();
                        Log($"[Recall] {card.DisplayName} Field→Reserve，当前SP={GetSp()}");
                        break;
                }
            }
            // 招式轮转 Fallback：当前卡不在 Field 中时重置
            if (CurrentZhaoShiCardId != -1 && !Field.Any(c => c.Id == CurrentZhaoShiCardId))
            {
                var list = GetZhaoShiList();
                CurrentZhaoShiCardId = list.Count > 0 ? list[0].Id : -1;
            }
        }
    }
}
