using System.Collections.ObjectModel;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>单场战斗独有的空间、灵元和扫描状态。没有世界数据引用。</summary>
    public sealed partial class CombatBackpack
    {
        // 初始化同一个背包实例；状态与操作按职责拆在其他 partial 文件中。
        public CombatBackpack(NpcCardData loadout)
        {
            var definition = NpcBackpackLayout.Validate(loadout);
            Width = definition.Width;
            Height = definition.Height;
            MainCardId = loadout.MainGongFaCardId!.Value;
            _validCells = definition.GetValidCells();
            foreach (var (id, cells) in loadout.Placements)
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
