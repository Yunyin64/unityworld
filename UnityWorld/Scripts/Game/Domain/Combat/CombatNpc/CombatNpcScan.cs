using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Combat
{
    public partial class CombatNpc
    {
        /// <summary>只在行时钟到点后调用；一行按列即时处理，成功使用才占额度。</summary>
        internal void ScanRow()
        {
            if (!IsActive) return;
            var rowActivationUsed = false;
            for (int x = 0; x < Backpack.Width; x++)
            {
                var cell = new BackpackCell(x, Backpack.Row);
                if (Backpack.Mana.ContainsKey(cell))
                {
                    // 选卡与转移统一由背包负责，候选卡仍由本次扫描传入。
                    var selected = Backpack.AssignManaAt(cell, Field);
                   // Log(selected == null ? $"格({x + 1},{cell.Y + 1}) 灵元留格" : $"格({x + 1},{cell.Y + 1}) 灵元进入 {selected.DefineId} 点亮={selected.LitCount}/{selected.GetSize()}");
                }
                if (Backpack.CardAt(cell) is not int id || !Backpack.IsCardCompleteAt(id, cell)) continue;
                Field.Single(c => c.Id == id).ResolveScanEnd(ref rowActivationUsed);
            }
        }
    }
}
