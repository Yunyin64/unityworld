using System.Collections.ObjectModel;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>单场战斗独有的空间、灵元和扫描状态。没有世界数据引用。</summary>
    public sealed partial class CombatBackpack
    {
        // 初始化同一个背包实例；状态与操作按职责拆在其他 partial 文件中。
        public CombatBackpack(NpcBackpackData layout, NpcCardData cards)
        {
            // 空间和卡牌选择分别复制；构造期间也不依赖世界功法管理器。
            _layout = layout.Clone();
            _cardData = cards.CloneSelectedCards();
            var definition = NpcSystemBackpack.Validate(_layout, _cardData);
            _validCells = definition.GetValidCells();
            foreach (var (id, cells) in NpcSystemBackpack.GetPlacements(_layout, _cardData))
            {
                var ordered = cells.OrderBy(c => c.Y).ThenBy(c => c.X).ToArray();
                _placements.Add(id, Array.AsReadOnly(ordered));
                foreach (var cell in ordered) _cardCells.Add(cell, id);
            }
            Mana = new ReadOnlyDictionary<BackpackCell, ElementType>(_mana);
            Placements = new ReadOnlyDictionary<int, IReadOnlyList<BackpackCell>>(_placements);
        }
    }
}
