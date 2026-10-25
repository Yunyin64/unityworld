using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>世界侧布局校验。只记录空间，不持有任何战斗资源或扫描进度。</summary>
    public static class NpcBackpackLayout
    {
        public static BackpackLevelDefine GetDefinition(NpcCardData data)
        {
            if (data.MainGongFaCardId is not int mainId || !data.AllCardIds.Contains(mainId))
                throw new InvalidOperationException("背包必须显式选择一张已持有的主功法卡。");
            var gongFa = GongFaMgr.Instance.GetById(mainId);
            var define = gongFa == null ? null : CultivationDefineMgr.Instance.Get(gongFa.DefineId);
            var levels = define?.BackpackLevels.Where(d => d.Level == data.MainGongFaLevel).ToList();
            if (levels == null || levels.Count != 1)
                throw new InvalidOperationException($"主功法 {mainId} 等级 {data.MainGongFaLevel} 必须有且仅有一个背包定义。");
            return levels[0].Clone();
        }

        /// <summary>验证形状、资源总数；被动卡也必须占用正数格子。</summary>
        public static void ValidateCard(Card card)
        {
            var data = card.BaseData;
            if (data.Size < 1 || data.Shape.Count != data.Size || data.Shape.Distinct().Count() != data.Size ||
                data.Shape.Any(p => p.X < 0 || p.Y < 0) || data.Shape.Min(p => p.X) != 0 || data.Shape.Min(p => p.Y) != 0)
                throw new InvalidOperationException($"卡 {card.DefineId}/{card.Id} 的形状必须规范化、无重复且占格数等于正数 Size。");
            if (data.ScanActivatable && (data.ManaCost.Values.Any(n => n <= 0) || data.ManaCost.Values.Sum() != data.Size))
                throw new InvalidOperationException($"卡 {card.DefineId}/{card.Id} 的激活灵元总数必须等于 Size。");
        }

        /// <summary>校验装配快照；编辑中的布局可暂有未放置 Field 卡，战斗入口必须完整。</summary>
        public static BackpackLevelDefine Validate(NpcCardData data, bool requireComplete = true)
        {
            var definition = GetDefinition(data);
            var valid = definition.GetValidCells();
            var mainId = data.MainGongFaCardId!.Value;
            if (data.AllCardIds.Distinct().Count() != data.AllCardIds.Count ||
                data.AllCards.Select(c => c.Id).Distinct().Count() != data.AllCards.Count ||
                !data.AllCardIds.ToHashSet().SetEquals(data.AllCards.Select(c => c.Id)))
                throw new InvalidOperationException("持有卡索引重复或不一致。");
            if (data.Field.Distinct().Count() != data.Field.Count || data.Reserve.Distinct().Count() != data.Reserve.Count ||
                data.Field.Intersect(data.Reserve).Any() || data.Field.Concat(data.Reserve).Any(id => !data.AllCardIds.Contains(id)))
                throw new InvalidOperationException("Field/Reserve 卡牌重复、交叉或未持有。");
            if (!data.Field.Contains(mainId))
                throw new InvalidOperationException("主功法必须在 Field 内。");
            if (!data.Placements.TryGetValue(mainId, out var fixedCells) || !fixedCells.ToHashSet().SetEquals(definition.MainCardCells))
                throw new InvalidOperationException("主功法固定位置不能移动或省略。");
            if (requireComplete && !data.Placements.Keys.ToHashSet().SetEquals(data.Field))
                throw new InvalidOperationException("每张 Field 卡必须有且仅有一份摆放，Reserve 卡不能占格。");
            var occupied = new HashSet<BackpackCell>();
            foreach (var (id, cells) in data.Placements)
            {
                if (!data.Field.Contains(id)) throw new InvalidOperationException($"摆放的卡 {id} 不在 Field。");
                var card = data.AllCards.Single(c => c.Id == id);
                ValidateCard(card);
                if (cells.Count != card.GetSize() || cells.Distinct().Count() != cells.Count)
                    throw new InvalidOperationException($"卡 {id} 占格数量不等于 Size，或存在重复格。");
                if (cells.Any(c => !valid.Contains(c))) throw new InvalidOperationException($"卡 {id} 越出背包有效空间。");
                var origin = new BackpackCell(cells.Min(c => c.X), cells.Min(c => c.Y));
                var relative = cells.Select(c => new BackpackCell(c.X - origin.X, c.Y - origin.Y)).ToHashSet();
                if (!relative.SetEquals(card.BaseData.Shape)) throw new InvalidOperationException($"卡 {id} 摆放不符合定义形状。");
                if (cells.Any(c => !occupied.Add(c))) throw new InvalidOperationException($"卡 {id} 与其他卡重叠。");
            }
            return definition;
        }
    }

    public partial class Npc
    {
        /// <summary>读取独立装配快照，布局集合的修改不会回写 NPC。</summary>
        public NpcCardData GetCardLoadoutSnapshot() => CardData.Clone();

        /// <summary>显式选择主功法及等级，固定占格并放入 Field；校验失败不修改现有布局。</summary>
        public void SelectMainGongFa(int cardId, int level = 1)
        {
            var candidate = CardData.Clone();
            if (candidate.MainGongFaCardId is int oldId && oldId != cardId)
            {
                candidate.Placements.Remove(oldId);
                candidate.Field.Remove(oldId);
                if (!candidate.Reserve.Contains(oldId)) candidate.Reserve.Add(oldId);
            }
            candidate.MainGongFaCardId = cardId;
            candidate.MainGongFaLevel = level;
            var definition = NpcBackpackLayout.GetDefinition(candidate);
            candidate.Reserve.Remove(cardId);
            if (!candidate.Field.Contains(cardId)) candidate.Field.Add(cardId);
            candidate.Placements[cardId] = [.. definition.MainCardCells];
            NpcBackpackLayout.Validate(candidate, false);
            CommitBackpackLayout(candidate);
        }

        /// <summary>按定义形状从原点摆放普通卡，同时由 Reserve/未分配移入 Field。</summary>
        public void PlaceBackpackCard(int cardId, int x, int y)
        {
            if (CardData.MainGongFaCardId == cardId) throw new InvalidOperationException("主功法固定卡不可移动。");
            var candidate = CardData.Clone();
            var card = candidate.AllCards.SingleOrDefault(c => c.Id == cardId)
                ?? throw new InvalidOperationException($"未持有卡 {cardId}。");
            NpcBackpackLayout.ValidateCard(card);
            candidate.Reserve.Remove(cardId);
            if (!candidate.Field.Contains(cardId)) candidate.Field.Add(cardId);
            candidate.Placements[cardId] = card.BaseData.Shape.Select(c => new BackpackCell(c.X + x, c.Y + y)).ToList();
            NpcBackpackLayout.Validate(candidate, false);
            CommitBackpackLayout(candidate);
        }

        public BackpackLevelDefine ValidateBackpackLayout() => NpcBackpackLayout.Validate(CardData);

        private void CommitBackpackLayout(NpcCardData candidate)
        {
            CardData.MainGongFaCardId = candidate.MainGongFaCardId;
            CardData.MainGongFaLevel = candidate.MainGongFaLevel;
            CardData.Field = candidate.Field;
            CardData.Reserve = candidate.Reserve;
            CardData.Placements = candidate.Placements;
        }
    }
}
