using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatNpc
    {
        /// <summary>只在行时钟到点后调用；一行按列即时处理，成功使用才占额度。</summary>
        internal void ScanRow()
        {
            if (!IsActive) return;
            var used = false;
            for (int x = 0; x < Backpack.Width; x++)
            {
                var cell = new BackpackCell(x, Backpack.Row);
                if (Backpack.Mana.ContainsKey(cell))
                {
                    var selected = ManaTargetSelector.AssignAt(Backpack, cell, Field);
                    Log(selected == null ? $"格({x},{cell.Y}) 灵元留格" : $"格({x},{cell.Y}) 灵元进入 {selected.DefineId} 点亮={Backpack.LitCount(selected.Id)}/{selected.GetSize()}");
                }
                if (Backpack.CardAt(cell) is not int id || !Backpack.IsCardCompleteAt(id, cell)) continue;
                var card = Field.Single(c => c.Id == id);
                if (!card.BaseData.ScanActivatable) continue;
                if (used) { Log($"格({x},{cell.Y}) {card.DefineId} 本行机会已用"); continue; }
                if (!Backpack.HasActivationResources(card)) { Log($"格({x},{cell.Y}) {card.DefineId} 未亮满，继续扫描"); continue; }
                used = card.TryUseFromScan();
                Log($"格({x},{cell.Y}) {card.DefineId} {(used ? "使用" : "待发占位，保持点亮等待")}");
            }
        }
    }
}
