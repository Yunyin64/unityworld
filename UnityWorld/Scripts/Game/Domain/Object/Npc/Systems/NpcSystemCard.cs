using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC卡牌/能力系统
    /// </summary>
    public class NpcSystemCard : NpcSystemBase<NpcCardData>
    {
        /// <summary>按角色编号保存的持卡数据</summary>
        protected override Dictionary<int, NpcCardData> _dataTable { get; set ; } = new();

        /// <summary>
        /// NPC 诞生时：创建空卡牌数据并注册
        /// </summary>
        public override void OnEntityBorn(BirthContext context)
        {
            var npc = context.MainNpc;
            var cardData = new NpcCardData();
            Register(npc, cardData);
            if (context.AssemblySource == NpcAssemblySource.Template) return;

            var InitCardDeck = context.Get<string[]>("InitCardDeck");
            if (InitCardDeck != null && InitCardDeck.Length > 0)
            {
                foreach (var cardDefineId in InitCardDeck) npc.GainCard(cardDefineId);
            }

            var initEquip = context.Get<List<InitEquipEntry>>("InitEquip");
            if (initEquip != null && initEquip.Count > 0)
            {
                foreach (var entry in initEquip)
                {
                    var card = npc.GainEquip(entry.CardID, entry.EquipID);
                    if (card != null) npc.EquipFaBao(card.Id);
                }
            }
        }

        /// <summary>从定义创建卡牌并记入角色的持卡数据</summary>
        public Card GainCard(NpcCardData data, string cardDefineId)
        {
            var card = CardMgr.Instance.InstantiateFromDefine(cardDefineId);
            return GainCard(data, card);
        }
        
        /// <summary>登记一张现有的卡及其实例编号</summary>
        public Card GainCard(NpcCardData data, Card card)
        {
            if (card == null) throw new InvalidOperationException("不能获得不存在的卡定义。");
            data.AllCardIds.Add(card.Id);
            data.AllCards.Add(card);
            return card;
        }

        /// <summary>验证形状、资源总数；被动卡也必须占用正数格子。</summary>
        public static void ValidateCard(Card card)
        {
            ValidateConsumeSelection(card);
            var data = card.BaseData;
            // 卡自身的形状、资源和兼容性约束统一由卡牌系统维护。
            var issues = new List<string>();
            var mana = data.ManaCost.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value);
            if (data.CompatibilityStatus != "Migrated")
                issues.Add($"不可装配：{data.CompatibilityStatus}，{data.CompatibilityReason}");
            if (data.Keywords?.Any(k => k is "Min" or "Max" || k?.StartsWith("ManaAdj", StringComparison.Ordinal) == true) == true ||
                mana.Keys.Any(k => k is "Min" or "Max"))
                issues.Add("依赖 Deprecated 的 Min/Max/ManaAdj，禁止装配。");
            var shape = data.Shape;
            if (shape == null || data.Size < 1 || shape.Count != data.Size || shape.Distinct().Count() != data.Size ||
                shape.Any(p => p.X < 0 || p.Y < 0) || shape.Count > 0 && (shape.Min(p => p.X) != 0 || shape.Min(p => p.Y) != 0))
                issues.Add("形状必须规范化、无重复且占格数等于正数 Size。");
            if (!CardKeywordRoles.TryGetMain(data.Keywords, out var main, out var mainError))
                issues.Add(mainError);
            else if (main != CardMainKeyword.Passive && (mana.Values.Any(n => n <= 0) || mana.Values.Sum(n => (long)n) != data.Size))
                issues.Add("主动卡的激活灵元总数必须等于 Size，且各需求为正数。");
            if (main == CardMainKeyword.ZhaoShi && data.MaxUsesPerRound < 1)
                issues.Add("招式每轮成功发动上限必须为正数。");
            if (issues.Count > 0)
                throw new InvalidOperationException($"卡 {card.DefineId}/{card.Id}：" + string.Join("；", issues));
        }

        /// <summary>校验消耗卡尚有合法的剩余次数可用于备战</summary>
        internal static void ValidateConsumeSelection(Card card)
        {
            if (card.HasKeyword("Consume") && (card.GetStack() <= 0 || card.GetStack() > card.GetStackMax()))
                throw new InvalidOperationException($"消耗卡 {card.DefineId}/{card.Id} 剩余 {card.GetStack()}/{card.GetStackMax()}，不能加入备战。");
        }

        /// <summary>取得可修改的世界卡数据，战斗子类不能经由此入口修改世界。</summary>
        internal NpcCardData GetWorldData(Npc npc)
        {
            npc.EnsureWorldCardOperation();
            var data = GetData(npc.Id);
            if (data.AllCardIds.Distinct().Count() != data.AllCardIds.Count ||
                data.AllCards.Select(card => card.Id).Distinct().Count() != data.AllCards.Count ||
                !data.AllCardIds.ToHashSet().SetEquals(data.AllCards.Select(card => card.Id)))
                throw new InvalidOperationException("持有卡索引重复或不一致。");
            return data;
        }

        /// <summary>阻止移除当前主功法卡或将其调至候补</summary>
        private static void GuardMainCard(NpcCardData data, int cardId)
        {
            if (data.MainGongFaCardId == cardId)
                throw new InvalidOperationException("不能移除或候补当前主功法，请先显式选择另一主功法。");
        }

        /// <summary>删除持有卡和分组标记；背包系统仅配合清理空间位置。</summary>
        public void RemoveCard(Npc npc, Card card)
        {
            var data = GetWorldData(npc);
            GuardMainCard(data, card.Id);
            NpcMgr.Instance.BackpackSystem.RemovePlacement(npc, card.Id);
            data.AllCardIds.Remove(card.Id);
            data.AllCards.Remove(card);
            data.Field.Remove(card.Id);
            data.Reserve.Remove(card.Id);
            data.EquippedFaBao.Remove(card.Id);
        }

        /// <summary>更新上场标记；已选主功法时要求背包已有卡位。</summary>
        public void AssignToField(Npc npc, int cardId)
        {
            var data = GetWorldData(npc);
            if (!data.AllCardIds.Contains(cardId)) throw new InvalidOperationException($"未持有卡 {cardId}。");
            ValidateConsumeSelection(data.AllCards.Single(card => card.Id == cardId));
            if (data.MainGongFaCardId != null && NpcMgr.Instance.BackpackSystem.GetData(npc.Id).GetPosition(cardId) == null)
                throw new InvalidOperationException("背包卡请通过 PlaceBackpackCard 显式指定位置。");
            data.Reserve.Remove(cardId);
            if (!data.Field.Contains(cardId)) data.Field.Add(cardId);
        }

        /// <summary>将持有卡标记为候补，并通知背包卸下其位置。</summary>
        public void AssignToReserve(Npc npc, int cardId)
        {
            var data = GetWorldData(npc);
            if (!data.AllCardIds.Contains(cardId)) throw new InvalidOperationException($"未持有卡 {cardId}。");
            GuardMainCard(data, cardId);
            ValidateConsumeSelection(data.AllCards.Single(card => card.Id == cardId));
            NpcMgr.Instance.BackpackSystem.RemovePlacement(npc, cardId);
            data.Field.Remove(cardId);
            if (!data.Reserve.Contains(cardId)) data.Reserve.Add(cardId);
        }

        /// <summary>清除上场和候补标记，同时卸下位置，持有关系不变。</summary>
        public void UnassignCard(Npc npc, int cardId)
        {
            var data = GetWorldData(npc);
            GuardMainCard(data, cardId);
            NpcMgr.Instance.BackpackSystem.RemovePlacement(npc, cardId);
            data.Field.Remove(cardId);
            data.Reserve.Remove(cardId);
        }

        /// <summary>按 Field 标记统计持有卡的总 Size。</summary>
        public int GetFieldSize(NpcCardData data)
        {
            int size = 0;
            foreach (var cardId in data.Field)
            {
                var card = data.AllCards.Find(c => c.Id == cardId);
                if (card != null) size += card.GetSize();
            }
            return size;
        }

        /// <summary>汇总角色持有的全部卡牌体量</summary>
        public int GetAllCardSize(NpcCardData data)
        {
            int size = 0;
            foreach (var card in data.AllCards)
            {
                size += card.GetSize(); 
            }
            return size;
        }

        /// <summary>保留角色持卡系统的时步入口</summary>
        public override void OnTick(Npc npc, float deltaTime)
        {
            // TODO: 卡牌系统逻辑
        }

        /// <summary>
        /// 获得法宝及其自带招式，绑定具体装备实例。位置由装配时确定。
        /// </summary>
        public Card GainEquip(NpcCardData data, string cardDefineId, string equipDefineId)
        {
            var equipDef = EquipDefineMgr.Instance?.Get(equipDefineId)
                ?? throw new InvalidOperationException($"找不到装备定义 {equipDefineId}。");
            var equip = Equip.FromDefine(equipDef);
            var card = CardMgr.Instance.InstantiateFromDefine(cardDefineId)
                ?? throw new InvalidOperationException($"找不到装备卡定义 {cardDefineId}。");
            equip.Id = card.Id;
            var forms = new List<Card>();
            try
            {
                forms = CreateMissingEquipmentForms(data, equip);
                EquipMgr.Instance.Add(card.Id, equip);
            }
            catch
            {
                foreach (var form in forms) CardMgr.Instance.UnregisterCard(form.Id);
                CardMgr.Instance.UnregisterCard(card.Id);
                throw;
            }

            GainCard(data, card);
            foreach (var form in forms) GainCard(data, form);
            return card;
        }

        /// <summary>装备法宝，按实例复用持有的招式。生效清单变化时只补发缺少的实例。</summary>
        public void EquipFaBao(NpcCardData data, int fabaoCardId)
        {
            var equip = EquipMgr.Instance?.GetById(fabaoCardId);
            if (equip == null || !data.AllCardIds.Contains(fabaoCardId))
                throw new InvalidOperationException($"不能装备未持有或没有装备实例的卡 {fabaoCardId}。");

            foreach (var form in CreateMissingEquipmentForms(data, equip)) GainCard(data, form);
            if (!data.EquippedFaBao.Contains(fabaoCardId))
                data.EquippedFaBao.Add(fabaoCardId);
        }

        /// <summary>补齐装备清单中尚未绑定到父卡的招式</summary>
        private static List<Card> CreateMissingEquipmentForms(NpcCardData data, Equip equip)
        {
            if (equip.FormList == null)
                throw new InvalidOperationException($"装备 {equip.DefineId}/{equip.Id} 的招式清单无效。");

            var existing = data.AllCards.Where(card => card.Id != equip.Id && card.ParentCardId == equip.Id &&
                    CardKeywordRoles.RequireMain(card.BaseData.Keywords, card.DefineId) == CardMainKeyword.ZhaoShi)
                .GroupBy(card => card.DefineId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var created = new List<Card>();
            try
            {
                // 每个清单条目对应一张卡，同名招式只能消耗该父装备名下的一份持有记录。
                foreach (var defineId in equip.FormList)
                {
                    if (string.IsNullOrWhiteSpace(defineId))
                        throw new InvalidOperationException($"装备 {equip.DefineId}/{equip.Id} 的招式 ID 不能为空。");
                    int count = existing.GetValueOrDefault(defineId);
                    if (count > 0)
                    {
                        existing[defineId] = count - 1;
                        continue;
                    }

                    var define = CardDefineMgr.Instance?.Get(defineId);
                    if (define == null || !CardKeywordRoles.TryGetMain(define.Keywords, out var main, out _) || main != CardMainKeyword.ZhaoShi)
                        throw new InvalidOperationException($"装备 {equip.DefineId}/{equip.Id} 的招式 {defineId} 缺少有效招式定义。");
                    var form = CardMgr.Instance.InstantiateFromDefine(define);
                    form.ParentCardId = equip.Id;
                    created.Add(form);
                }
            }
            catch
            {
                foreach (var form in created) CardMgr.Instance.UnregisterCard(form.Id);
                throw;
            }
            return created;
        }

        /// <summary>
        /// 卸装法宝（移除标记）
        /// </summary>
        public void UnequipFaBao(NpcCardData data, int fabaoCardId)
        {
            data.EquippedFaBao.Remove(fabaoCardId);
        }
    }

    public partial class Npc
    {
        /// <summary>世界卡牌和装配写入口的共同保护，防止战斗子类误改世界。</summary>
        internal void EnsureWorldCardOperation()
        {
            if (this is CombatNpc)
                throw new InvalidOperationException("战斗中请操作 CombatBackpack/CombatCard，不能通过 NPC 入口修改世界持有卡或战前装配。");
        }

        /// <summary>检查完整装配后复制本场所选卡，避免裁掉未放置的装备而漏报其招式。</summary>
        public NpcCardData GetCombatCardSnapshot()
        {
            ValidateBackpackLayout();
            return CardData.CloneSelectedCards();
        }
        /// <summary>读取卡数据中的上场标记。</summary>
        public List<int> GetFieldIds() => [.. CardData.Field];
        /// <summary>读取明确选定的候补标记。</summary>
        public List<int> GetReserveIds() => [.. CardData.Reserve];
        /// <summary>获取上场卡的 SP 占用。</summary>
        public int GetFieldSize() => NpcMgr.Instance.CardSystem.GetFieldSize(CardData);
        /// <summary>将卡标记为上场。</summary>
        public void AssignToField(int cardId) => NpcMgr.Instance.CardSystem.AssignToField(this, cardId);
        /// <summary>将卡标记为候补。</summary>
        public void AssignToReserve(int cardId) => NpcMgr.Instance.CardSystem.AssignToReserve(this, cardId);
        /// <summary>取消上场和候补选择。</summary>
        public void UnassignCard(int cardId) => NpcMgr.Instance.CardSystem.UnassignCard(this, cardId);

        /// <summary>从定义创建新卡并加入本角色的持有列表</summary>
        public Card GainCard(string cardDefineId)
        {
            EnsureWorldCardOperation();
            return NpcMgr.Instance.CardSystem.GainCard(CardData, cardDefineId);
        }
        /// <summary>把现有卡加入本角色的持有列表</summary>
        public void GainCard(Card card)
        {
            EnsureWorldCardOperation();
            NpcMgr.Instance.CardSystem.GainCard(CardData, card);
        }
        /// <summary>从本角色移除卡并同步备战数据</summary>
        public void RemoveCard(Card card)
        {
            EnsureWorldCardOperation();
            NpcMgr.Instance.CardSystem.RemoveCard(this, card);
        }
        /// <summary>汇总本角色所有持有卡的体量</summary>
        public int GetAllCardSize() => NpcMgr.Instance.CardSystem.GetAllCardSize(CardData);
        /// <summary>装备法宝并补齐该装备实例的持有招式。</summary>
        public void EquipFaBao(int fabaoCardId)
        {
            EnsureWorldCardOperation();
            NpcMgr.Instance.CardSystem.EquipFaBao(CardData, fabaoCardId);
        }
        /// <summary>卸装法宝</summary>
        public void UnequipFaBao(int fabaoCardId)
        {
            EnsureWorldCardOperation();
            NpcMgr.Instance.CardSystem.UnequipFaBao(CardData, fabaoCardId);
        }

        /// <summary>获得法宝（Card + Equip），返回 Card 实例</summary>
        public Card GainEquip(string cardDefineId, string equipDefineId)
        {
            EnsureWorldCardOperation();
            return NpcMgr.Instance.CardSystem.GainEquip(CardData, cardDefineId, equipDefineId);
        }

        /// <summary>
        /// 测试用：将所有持有卡牌全部塞入运转池（Field）。
        /// 装备自带招式已在获得或装备时发放。
        /// </summary>
        public void AssignAllToField()
        {
            EnsureWorldCardOperation();
            if (CardData.MainGongFaCardId != null)
                throw new InvalidOperationException("背包布局必须逐张显式摆放，不能 AssignAllToField。");
            foreach (var card in CardData.AllCards) NpcSystemCard.ValidateConsumeSelection(card);
            // 先把已有卡全入 Field
            foreach (var cardId in CardData.AllCardIds)
            {
                if (!CardData.Field.Contains(cardId))
                    CardData.Field.Add(cardId);
            }
            CardData.Reserve.Clear();
        }
    }
}
