using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Combat
{
    /// <summary>
    /// CombatCard 的 CardModifier 管理逻辑（partial）：
    /// AddCardBuff / ModifierTick / RemoveCardBuff / GetStat 贡献
    /// </summary>
    public partial class CombatCard
    {
        private List<CardModifier> CardModifiers { get; set; } = new();

        // ══════════════════════════════════════════════════════════
        //  AddCardBuff
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 添加 CardModifier。同 Id 已存在时执行叠层逻辑。
        /// </summary>
        public void AddCardBuff(CardModifier modifier)
        {
            if (modifier == null) return;

            // 查重：同 Id 叠层
            var existing = CardModifiers.FirstOrDefault(m => m.Id == modifier.Id);
            if (existing != null)
            {
                existing.AddStack(modifier.CurrentStack);
                Log($"[CardBuff] 叠层: {modifier.Id} (Stack={existing.CurrentStack})");
                return;
            }

            CardModifiers.Add(modifier);
            Log($"[CardBuff] 添加: {modifier.Id} (Stack={modifier.CurrentStack}, Expire={modifier.ExpirePolicy})");
        }

        // ══════════════════════════════════════════════════════════
        //  RemoveCardBuff
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 主动移除指定 Id 的 CardModifier。未找到时静默跳过。
        /// </summary>
        public void RemoveCardBuff(string id)
        {
            var modifier = CardModifiers.FirstOrDefault(m => m.Id == id);
            if (modifier == null) return;
            CardModifiers.Remove(modifier);
            Log($"[CardBuff] 移除: {id}");
        }

        // ══════════════════════════════════════════════════════════
        //  ModifierTick
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 每战斗 Tick 驱动所有 CardModifier：衰减时间、移除过期。
        /// </summary>
        public void CardModifierTick()
        {
            if (CardModifiers.Count == 0) return;

            foreach (var mod in CardModifiers)
            {
                if (mod.ExpirePolicy == ExpirePolicy.TimeBased || mod.ExpirePolicy == ExpirePolicy.TimeOrStack)
                {
                    if (mod.RemainingTime > 0)
                        mod.RemainingTime--;
                }
            }

            var toRemove = CardModifiers.Where(m => m.IsExpired()).ToList();
            foreach (var mod in toRemove)
            {
                CardModifiers.Remove(mod);
                Log($"[CardBuff] 过期移除: {mod.Id}");
            }
        }

        // ══════════════════════════════════════════════════════════
        //  Stat 贡献
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 累计所有 CardModifier 对指定属性的贡献值。
        /// </summary>
        public float GetCardModifierStat(string statId)
        {
            float total = 0f;
            foreach (var mod in CardModifiers)
            {
                if (mod.StatModifiers == null) continue;
                foreach (var entry in mod.StatModifiers)
                {
                    if (entry.StatId == statId)
                    {
                        total += entry.Value * mod.CurrentStack;
                    }
                }
            }
            return total;
        }
    }
}
