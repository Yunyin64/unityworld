using UnityWorld.Core;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 战斗域的 API 函数实现集合。
    /// 每个方法以 [APIFunc] 标记，由 APIMgr 反射扫描自动注册。
    /// 从  ContextBase.Env 中取 "Caster"(CombatNpc) 和 "Target"(CombatNpc) 作为操作主体。
    /// </summary>
    public  static partial class CombatBaseFunc
    {

        // ── 效果类 ────────────────────────────────────────────

        /// <summary>恢复战斗中HP。参数：Domain(String), HealValue(Int)</summary>
        [APIFunc("Heal", APIType.Action, "恢复战斗中HP", Scope.CombatNpc, "Domain:String", "HealValue:Int")]
        public static APIContext Heal(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            int healValue = ctx.GetValue("HealValue", 0);
            foreach (var npc in ctx.NpcTargets)
            {
                npc.ApplyHeal(healValue);
            }
            return ctx;
        }

        /// <summary>自伤。参数：Domain(String), DamageValue(Int)</summary>
        [APIFunc("SelfDamage", APIType.Action, "自伤", Scope.CombatNpc, "Domain:String", "DamageValue:Int")]
        public static APIContext SelfDamage(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            int damageValue = ctx.GetValue("DamageValue", 0);
            foreach (var npc in ctx.NpcTargets)
            {
                var dmg = new DamageInfo();
                dmg.Damage = damageValue;
                npc.AddDamage(dmg);
            }
            return ctx;
        }



        // ── 破甲/护盾类 ──────────────────────────────────────

        /// <summary>破甲：消除目标护盾值。参数：Domain(String), BreakValue(Int)</summary>
        [APIFunc("ArmorBreak", APIType.Action, "消除目标护盾值", Scope.CombatNpc, "Domain:String", "BreakValue:Int")]
        public static APIContext ArmorBreak(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            int breakValue = ctx.GetValue("BreakValue", 0);
            foreach (var npc in ctx.NpcTargets)
            {
                int actual = Math.Min(npc.ShieldValue, breakValue);
                if (actual > 0)
                    npc.ChangeShield(-actual);
            }
            return ctx;
        }

        // ── Buff 类 ──────────────────────────────────────────

        /// <summary>给目标NPC添加Buff。参数：Domain(String), BuffId(String), Stacks(Int), Duration(Float)</summary>
        [APIFunc("AddNpcBuff", APIType.Action, "给目标NPC添加Buff", Scope.CombatNpc, "Domain:String", "BuffId:String", "Stacks:Int", "Duration:Float")]
        public static void AddNpcBuff(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            string buffId = ctx.GetValue("BuffId", "");
            int stacks = ctx.GetValue("Stacks", 1);
            float duration = ctx.GetValue("Duration", -1f);

            if (string.IsNullOrEmpty(buffId)) return;

            foreach (var npc in ctx.NpcTargets)
            {
                npc.AddModifier(buffId, stacks, duration);
            }
        }

        // ── 轻量属性修正类 ──────────────────────────────────────

        /// <summary>给目标添加永久属性修正。参数：Domain(String), StatId(String), Value(Float), ModifierType(String), SourceId(String)</summary>
        [APIFunc("AddStatBuff", APIType.Action, "给目标添加永久属性修正", Scope.CombatNpc, "Domain:String", "StatId:String", "Value:Float", "ModifierType:String", "SourceId:String")]
        public static APIContext AddStatBuff(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            string statId = ctx.GetValue("StatId", "");
            float value = ctx.GetValue("Value", 0f);
            string modifierType = ctx.GetValue("ModifierType", "Flat");
            string sourceId = ctx.GetValue("SourceId", "");

            if (!Enum.TryParse<ModifierType>(modifierType, true, out var type))
            {
                LogMgr.Instance.Warn($"[StatBuff] 无法解析 ModifierType: '{modifierType}'，已忽略");
                return ctx;
            }

            if (string.IsNullOrEmpty(statId)) return ctx;

            foreach (var npc in ctx.NpcTargets)
            {
                npc.AddStatBuff(statId, value, type, string.IsNullOrEmpty(sourceId) ? null : sourceId);
            }
            return ctx;
        }

        // ── 卡组操作类 ────────────────────────────────────────

        /// <summary>移除目标随机一张伤势卡（按体量筛选）。参数：Domain(String), Size(Int), Exact(Bool)</summary>
        [APIFunc("RemoveRandomWound", APIType.Action, "移除目标随机一张伤势卡", Scope.CombatNpc, "Domain:String", "Size:Int", "Exact:Bool")]
        public static APIContext RemoveRandomWound(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.NpcTargets = APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx);

            int size = ctx.GetValue("Size", 1);
            bool exact = ctx.GetValue("Exact", true);

            foreach (var npc in ctx.NpcTargets)
            {
                var deck = npc.GetField();
                var wounds = deck.Where(c => c.HasKeyword("Wound") && (exact ? c.GetSize() == size : c.GetSize() <= size)).ToList();
                if (wounds.Count == 0) continue;

                var picked = wounds[npc.Scene.Soul.Random(0, wounds.Count)];
                npc.RemoveCombatCard(picked);
                LogMgr.Instance.Dbg("[RemoveRandomWound] {0} 移除伤势卡: {1} (Size:{2})", npc.GetName(), picked.DisplayName, picked.GetSize());
            }

            return ctx;
        }

        /// <summary>位移目标卡牌到指定位置。参数：Domain(String), Position(String: First/Last/Random)</summary>
        [APIFunc("Displace", APIType.Action, "位移目标卡牌", Scope.CombatCard, "Domain:String", "Position:String")]
        public static APIContext Displace(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return ctx;
            ctx.CardTargets = APIMgr.Instance.GetTargetCard(ctx.GetStringValue("Domain"), ctx);

            string posStr = ctx.GetValue("Position", "Random");
            if (!Enum.TryParse<ComabtCardDisplaceType>(posStr, true, out var pos))
                pos = ComabtCardDisplaceType.Random;

            foreach (var card in ctx.CardTargets)
            {
                var owner = card.Owner;
                if (owner == null) continue;
                owner.DisplaceCombatCard(card, pos);
                LogMgr.Instance.Dbg("[Displace] {0} 的卡牌 {1} 位移至 {2}", owner.GetName(), card.DisplayName, pos);
            }
            return ctx;
        }

    }
}
