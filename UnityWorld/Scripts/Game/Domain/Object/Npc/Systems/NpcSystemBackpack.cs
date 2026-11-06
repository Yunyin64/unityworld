using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>管理 NPC 的空间和摆放；卡牌分组、装备和主功法标记仍保存在 NpcCardData。</summary>
    public class NpcSystemBackpack : NpcSystemBase<NpcBackpackData>
    {
        protected override Dictionary<int, NpcBackpackData> _dataTable { get; set; } = new();
        /// <summary>出生时注册独立的空装配。</summary>
        public override void OnEntityBorn(BirthContext context) => Register(context.MainNpc, new NpcBackpackData());
        /// <summary>普通装配不驱动战斗时钟。</summary>
        public override void OnTick(Npc npc, float deltaTime) { }
        /// <summary>世界结束时释放装配。</summary>
        public void Clear() => _dataTable.Clear();

        // 纯布局计算只使用传入参数，普通装配与独立战斗副本共用同一套规则。
        /// <summary>从已经解析的装配参数恢复空间定义，供世界和战斗共用。</summary>
        public static BackpackLevelDefine GetDefinition(NpcBackpackData data, NpcCardData cards) => new()
        {
            Level = cards.MainGongFaLevel, Width = data.Width, Height = data.Height,
            ValidCells = [.. data.ValidCells], MainCardCells = [.. data.MainCardCells]
        };

        /// <summary>由背包的锚点和卡的形状生成占格，卡实例不再保存位置。</summary>
        public static List<BackpackCell> CellsFromPosition(Card card, BackpackPosition position)
        {
            var origin = BackpackCoordinates.ToCell(position);
            return card.BaseData.Shape.Select(c => new BackpackCell(origin.X + c.X, origin.Y + c.Y)).ToList();
        }

        /// <summary>每次派生独立占格索引，摆放条目是位置的唯一来源。</summary>
        public static Dictionary<int, List<BackpackCell>> GetPlacements(NpcBackpackData data, NpcCardData cards)
        {
            var entries = data.PlacedCards.Where(entry => entry.Position != null).ToDictionary(entry => entry.CardId);
            // 沿用原来按持有卡顺序建立索引的行为，避免结构调整改变遍历顺序。
            return cards.AllCards.Where(card => entries.ContainsKey(card.Id))
                .ToDictionary(card => card.Id, card => CellsFromPosition(card, entries[card.Id].Position.Value));
        }

        /// <summary>校验完整装配；编辑候选允许非主卡暂未放置，战斗入口必须完整。</summary>
        public static BackpackLevelDefine Validate(NpcBackpackData data, NpcCardData cards, bool requireComplete = true)
        {
            var allCards = cards.AllCards;
            if (allCards.Select(card => card.Id).Distinct().Count() != allCards.Count ||
                cards.AllCardIds.Distinct().Count() != cards.AllCardIds.Count ||
                !cards.AllCardIds.ToHashSet().SetEquals(allCards.Select(card => card.Id)))
                throw new InvalidOperationException("持有卡索引重复或不一致。");
            var byId = allCards.ToDictionary(card => card.Id);
            if (cards.MainGongFaCardId is not int mainId || !byId.ContainsKey(mainId))
                throw new InvalidOperationException("背包必须显式选择一张已持有的主功法卡。");
            var definition = GetDefinition(data, cards);
            var valid = definition.GetValidCells();
            var field = cards.Field;
            if (field.Distinct().Count() != field.Count || cards.Reserve.Distinct().Count() != cards.Reserve.Count ||
                field.Intersect(cards.Reserve).Any() || field.Concat(cards.Reserve).Any(id => !byId.ContainsKey(id)))
                throw new InvalidOperationException("Field/Reserve 卡牌重复、交叉或未持有。");
            if (!field.Contains(mainId)) throw new InvalidOperationException("主功法必须在 Field 内。");
            if (data.PlacedCards.Select(entry => entry.CardId).Distinct().Count() != data.PlacedCards.Count ||
                data.PlacedCards.Any(entry => !field.Contains(entry.CardId)))
                throw new InvalidOperationException("摆放条目重复或不在 Field 内。");
            var placements = GetPlacements(data, cards);
            if (!placements.TryGetValue(mainId, out var fixedCells) || !fixedCells.ToHashSet().SetEquals(definition.MainCardCells))
                throw new InvalidOperationException($"主功法 {byId[mainId].DefineId}/{mainId} 位置 {data.GetPosition(mainId)} 与固定格冲突，不能移动或省略。");
            if (requireComplete && !placements.Keys.ToHashSet().SetEquals(field))
                throw new InvalidOperationException("每张 Field 卡必须有且仅有一份摆放，Reserve 卡不能占格。");
            var occupied = new HashSet<BackpackCell>();
            foreach (var (id, cells) in placements)
            {
                var card = byId[id];
                NpcSystemCard.ValidateCard(card);
                if (cells.Count != card.GetSize() || cells.Distinct().Count() != cells.Count)
                    throw new InvalidOperationException($"卡 {id} 占格数量不等于 Size，或存在重复格。");
                if (cells.Any(cell => !valid.Contains(cell)))
                    throw new InvalidOperationException($"卡 {card.DefineId}/{id} 位置 {data.GetPosition(id)} 越出背包有效空间。");
                if (cells.Any(cell => !occupied.Add(cell)))
                    throw new InvalidOperationException($"卡 {card.DefineId}/{id} 位置 {data.GetPosition(id)} 与其他卡重叠。");
            }
            return definition;
        }

        private (NpcCardData Cards, NpcBackpackData Layout) BeginEdit(Npc npc)
        {
            var cards = NpcMgr.Instance.CardSystem.GetWorldData(npc);
            // 只复制选择标记及布局；不改动或复制整仓库的卡实例。
            return (cards.CloneSelection(), GetData(npc.Id).Clone());
        }

        private void Commit(Npc npc, NpcCardData cards, NpcBackpackData layout)
        {
            NpcMgr.Instance.CardSystem.Register(npc, cards);
            Register(npc, layout);
        }

        private static Card RequireCard(NpcCardData cards, int cardId) => cards.AllCards.SingleOrDefault(card => card.Id == cardId)
            ?? throw new InvalidOperationException($"未持有卡 {cardId}。");

        // 主功法按现有配置直接写入空间参数，战斗阶段不再查询世界功法管理器。
        private static void ApplyMainDefinition(NpcBackpackData layout, NpcCardData cards)
        {
            if (cards.MainGongFaCardId is not int mainId) throw new InvalidOperationException("背包必须显式选择一张已持有的主功法卡。");
            RequireCard(cards, mainId);
            var gongFa = GongFaMgr.Instance.GetById(mainId);
            var define = gongFa == null ? null : CultivationDefineMgr.Instance.Get(gongFa.DefineId);
            if (define == null) throw new InvalidOperationException($"主功法 {mainId} 没有有效功法绑定。");
            var space = define.GetBackpackDefinition(cards.MainGongFaLevel);
            layout.Width = space.Width; layout.Height = space.Height;
            layout.ValidCells = space.GetValidCells().OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToList();
            layout.MainCardCells = [.. space.MainCardCells];
        }

        private static void SetPosition(NpcBackpackData layout, int cardId, BackpackPosition position)
        {
            var entry = layout.PlacedCards.SingleOrDefault(item => item.CardId == cardId);
            if (entry == null) layout.PlacedCards.Add(new() { CardId = cardId, Position = position });
            else entry.Position = position;
        }

        /// <summary>模板只加载空间并校验，不自动修正模板给出的主卡位置。</summary>
        public void InitializeTemplateLayout(Npc npc, int mainCardId)
        {
            var (cards, layout) = BeginEdit(npc);
            cards.MainGongFaCardId = mainCardId;
            ApplyMainDefinition(layout, cards);
            NpcSystemBackpack.Validate(layout, cards);
            Commit(npc, cards, layout);
        }

        /// <summary>选择主功法，原子更新卡牌标记和固定摆放。</summary>
        public void SelectMainGongFa(Npc npc, int cardId, int level)
        {
            var (cards, layout) = BeginEdit(npc);
            if (cards.MainGongFaCardId is int oldId && oldId != cardId)
            {
                layout.PlacedCards.RemoveAll(entry => entry.CardId == oldId);
                cards.Field.Remove(oldId);
                if (!cards.Reserve.Contains(oldId)) cards.Reserve.Add(oldId);
            }
            cards.MainGongFaCardId = cardId; cards.MainGongFaLevel = level;
            ApplyMainDefinition(layout, cards);
            cards.Reserve.Remove(cardId);
            if (!cards.Field.Contains(cardId)) cards.Field.Add(cardId);
            var anchor = new BackpackCell(layout.MainCardCells.Min(cell => cell.X), layout.MainCardCells.Min(cell => cell.Y));
            SetPosition(layout, cardId, BackpackCoordinates.ToPosition(anchor));
            NpcSystemBackpack.Validate(layout, cards, false);
            Commit(npc, cards, layout);
        }

        /// <summary>显式摆放持有卡，更新 Field/Reserve 标记；失败不提交任何候选数据。</summary>
        public void PlaceCard(Npc npc, int cardId, BackpackPosition position)
        {
            var (cards, layout) = BeginEdit(npc);
            if (cards.MainGongFaCardId == cardId) throw new InvalidOperationException("主功法固定卡不可移动。");
            NpcSystemCard.ValidateCard(RequireCard(cards, cardId));
            cards.Reserve.Remove(cardId);
            if (!cards.Field.Contains(cardId)) cards.Field.Add(cardId);
            SetPosition(layout, cardId, position);
            NpcSystemBackpack.Validate(layout, cards, false);
            Commit(npc, cards, layout);
        }

        /// <summary>配合卡牌系统卸下空间位置，不修改持有关系或任何卡牌标记。</summary>
        internal void RemovePlacement(Npc npc, int cardId)
        {
            npc.EnsureWorldCardOperation();
            var layout = GetData(npc.Id).Clone();
            layout.PlacedCards.RemoveAll(entry => entry.CardId == cardId);
            Register(npc, layout);
        }
    }

    public partial class Npc
    {
        /// <summary>读取独立的战前装配快照，不复制闲置持有卡。</summary>
        public NpcBackpackData GetBackpackSnapshot() => BackpackData.Clone();
        /// <summary>查询当前上下文的卡位；战斗子类使用本场布局。</summary>
        public BackpackPosition? GetBackpackCardPosition(int cardId) => BackpackData.GetPosition(cardId);
        /// <summary>取得派生的独立占格索引，不允许通过索引修改装配。</summary>
        public Dictionary<int, List<BackpackCell>> GetBackpackPlacements() => NpcSystemBackpack.GetPlacements(BackpackData, CardData);
        /// <summary>显式选择主功法和布局等级，失败保留原配置。</summary>
        public void SelectMainGongFa(int cardId, int level = 1) => NpcMgr.Instance.BackpackSystem.SelectMainGongFa(this, cardId, level);
        /// <summary>使用公开 1 起始坐标摆卡，位置归装配数据。</summary>
        public void PlaceBackpackCard(int cardId, int x, int y) => NpcMgr.Instance.BackpackSystem.PlaceCard(this, cardId, new(x, y));
        /// <summary>专用几何回归入口，接受内部零起始坐标。</summary>
        public void PlaceBackpackCardInternal(int cardId, int x, int y)
        {
            var position = BackpackCoordinates.ToPosition(new(x, y));
            PlaceBackpackCard(cardId, position.X, position.Y);
        }
        /// <summary>校验当前装配，不重新查询或修改主功法配置。</summary>
        public BackpackLevelDefine ValidateBackpackLayout() => NpcSystemBackpack.Validate(BackpackData, CardData);

    }
}
