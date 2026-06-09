using System.Collections;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatNpc
    {
        protected List<CombatCard> Field { get; set; } = new();

        /// <summary>候补池：静默，不Tick、不占SP</summary>
        protected List<CombatCard> Reserve { get; set; } = new();

        private Queue<(ComabtFieldChangeType,CombatCard,ComabtCardDisplaceType)> _changes = new();

        public List<CombatCard> GetField()
        {
            return Field;
        }

        /// <summary>获取候补池</summary>
        public List<CombatCard> GetReserve()
        {
            return Reserve;
        }

        public CombatCard GetCardByIndex(int index)
        {
            return Field[index];
        }

        public int GetIndexByCard(CombatCard card)
        {
            return Field.IndexOf(card);
        }

        public void RemoveCombatCard(CombatCard card)
        {
            _changes.Enqueue((ComabtFieldChangeType.Remove, card, ComabtCardDisplaceType.None));
        }

        public void AddCombatCard(CombatCard card)
        {
            _changes.Enqueue((ComabtFieldChangeType.Add, card, ComabtCardDisplaceType.None));
        }
        
        public void DisplaceCombatCard(CombatCard card, ComabtCardDisplaceType toPlace)
        {
            _changes.Enqueue((ComabtFieldChangeType.Displace, card, toPlace));
        }

        /// <summary>部署：将卡从 Reserve 移入 Field（延迟执行）</summary>
        public void Deploy(CombatCard card)
        {
            _changes.Enqueue((ComabtFieldChangeType.Deploy, card, ComabtCardDisplaceType.None));
        }

        /// <summary>召回：将卡从 Field 移入 Reserve（延迟执行）</summary>
        public void Recall(CombatCard card)
        {
            _changes.Enqueue((ComabtFieldChangeType.Recall, card, ComabtCardDisplaceType.None));
        }

        // ── 招式轮转 ──────────────────────────────────────────

        public List<CombatCard> GetZhaoShiList()
        {
            return Field.Where(c => c.HasKeyword("ZhaoShi")).ToList();
        }

        public int GetCurrentZhaoShiCardId()
        {
            return CurrentZhaoShiCardId;
        }

        public void SetCurrentZhaoShiCardId(int id)
        {
            CurrentZhaoShiCardId = id;
        }

        public void AdvanceZhaoShi()
        {
            var list = GetZhaoShiList();
            if (list.Count == 0)
            {
                CurrentZhaoShiCardId = -1;
                return;
            }
            var idx = list.FindIndex(c => c.Id == CurrentZhaoShiCardId);
            var nextIdx = (idx + 1) % list.Count;
            CurrentZhaoShiCardId = list[nextIdx].Id;
            list[nextIdx].ResetCD();
            Log($"[ZhaoShi]  切换招式为【{list[nextIdx].DisplayName}】");
        }

        public void InitZhaoShiRotation()
        {
            var list = GetZhaoShiList();
            CurrentZhaoShiCardId = list.Count > 0 ? list[0].Id : -1;
        }

        // ── 统一处理队列 ──────────────────────────────────────────

        public void DealFieldChange()
        {
            while (_changes.Count > 0)
            {
                var (changeType, card, displaceType) = _changes.Dequeue();
                switch (changeType)
                {
                    case ComabtFieldChangeType.Add:
                        Field.Add(card);
                        card.Owner = this;
                        Log($"[Field+] {card.DisplayName}，当前SP={GetSp()}");
                        break;

                    case ComabtFieldChangeType.Remove:
                        Field.Remove(card);
                        Log($"[Field-] {card.DisplayName}，当前SP={GetSp()}");
                        break;

                    case ComabtFieldChangeType.Displace:
                        Field.Remove(card);
                        switch (displaceType)
                        {
                            case ComabtCardDisplaceType.First:
                                Field.Insert(0, card);
                                break;
                            case ComabtCardDisplaceType.Last:
                                Field.Add(card);
                                break;
                            case ComabtCardDisplaceType.Random:
                                var idx = Field.Count > 0
                                    ? Scene.Soul.Random(0, Field.Count + 1)
                                    : 0;
                                Field.Insert(idx, card);
                                break;
                            default:
                                Field.Add(card);
                                break;
                        }
                        Log($"[Displace] {card.DisplayName} -> {displaceType}");
                        break;

                    case ComabtFieldChangeType.Deploy:
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

                    case ComabtFieldChangeType.Recall:
                        if (!Field.Contains(card))
                        {
                            Log($"[Recall] 警告：{card.DisplayName} 不在 Field 中，跳过");
                            break;
                        }
                        Field.Remove(card);
                        Reserve.Add(card);
                        card.CallLua("OnRecall");
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
