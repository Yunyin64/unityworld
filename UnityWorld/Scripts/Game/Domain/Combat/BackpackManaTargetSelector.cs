#nullable enable
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// 先筛选需要且能接收的卡，再排序。距离函数是预留策略；当前演示用到最近占格的曼哈顿距离，非最终玩法定案。
    /// 同距按候选卡最下行、最左列选取；完全重合排序键最后用实例 Id 保证重放稳定。
    /// </summary>
    public sealed class BackpackManaTargetSelector
    {
        public Func<BackpackCell, IReadOnlyList<BackpackCell>, int> Distance { get; }

        public BackpackManaTargetSelector(Func<BackpackCell, IReadOnlyList<BackpackCell>, int>? distance = null)
        {
            Distance = distance ?? ((source, cells) => cells.Min(c => Math.Abs(c.X - source.X) + Math.Abs(c.Y - source.Y)));
        }

        public CombatCard? Select(CombatBackpack backpack, BackpackCell source, ElementType element, IEnumerable<CombatCard> cards)
        {
            return cards.Where(c => backpack.CanAccept(c, element))
                .OrderBy(c => Distance(source, backpack.Placements[c.Id]))
                .ThenByDescending(c => backpack.Placements[c.Id].Max(p => p.Y))
                .ThenBy(c => backpack.Placements[c.Id].Min(p => p.X))
                .ThenBy(c => c.Id).FirstOrDefault();
        }

        /// <summary>没有可接受卡时保留在原格；已经转移的格再次处理不会重复分配。</summary>
        public CombatCard? AssignAt(CombatBackpack backpack, BackpackCell source, IEnumerable<CombatCard> cards)
        {
            if (!backpack.Mana.TryGetValue(source, out var element)) return null;
            var selected = Select(backpack, source, element, cards);
            return selected != null && backpack.TryAssign(source, selected) ? selected : null;
        }
    }
}
