using UnityWorld.Core;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    public class APIDomainFunc
    {
        private Dictionary<string, Func<APIContext, List<CombatCard>>> _cardFuncs = new();
        private Dictionary<string, Func<APIContext, List<CombatNpc>>> _npcFuncs = new();

        public APIDomainFunc()
        {
            // Card（按优先级排序：自身 → 己方 → 对方）
            _cardFuncs["Self"] = GetSelfCard;
            _cardFuncs["Random"] = GetRandomCard;
            _cardFuncs["Other"] = GetOtherCard;
            _cardFuncs["Adjacent"] = GetAdjacentCard;
            _cardFuncs["AboveOne"] = GetAboveOneCard;
            _cardFuncs["AboveAll"] = GetAboveAllCard;
            _cardFuncs["BelowOne"] = GetBelowOneCard;
            _cardFuncs["BelowAll"] = GetBelowAllCard;
            _cardFuncs["All"] = GetAllCard;
            _cardFuncs["TargetAll"] = GetTargetAllCard;
            _cardFuncs["TargetRandom"] = GetTargetRandomCard;

            // Npc
            _npcFuncs["Self"] = GetSelfNpc;
            _npcFuncs["Target"] = GetTargetNpc;
        }

        // ── Card ──────────────────────────────────────────

        private List<CombatCard> GetSelfCard(APIContext ctx)
        {
            if (ctx.SourceCard == null) return new();
            return new() { ctx.SourceCard };
        }

        private List<CombatCard> GetRandomCard(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return new();
            var cards = caster.GetField().Where(c => c.CheckPhase(CombatCardPhase.InCD)).ToList();
            if (cards.Count == 0) return new();
            var idx = caster.Scene.Soul.Random(0, cards.Count);
            return new() { cards[idx] };
        }

        private List<CombatCard> GetOtherCard(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return new();
            var source = ctx.SourceCard;
            return caster.GetField().Where(c => c != source).ToList();
        }

        private List<CombatCard> GetAdjacentCard(APIContext ctx)
        {
            var caster = ctx.Caster;
            var source = ctx.SourceCard;
            if (caster == null || source == null) return new();
            var field = caster.GetField();
            int index = caster.GetIndexByCard(source);
            if (index < 0) return new();
            var result = new List<CombatCard>();
            if (index > 0) result.Add(field[index - 1]);
            if (index < field.Count - 1) result.Add(field[index + 1]);
            return result;
        }

        private List<CombatCard> GetAboveOneCard(APIContext ctx)
        {
            var caster = ctx.Caster;
            var source = ctx.SourceCard;
            if (caster == null || source == null) return new();
            var field = caster.GetField();
            int index = caster.GetIndexByCard(source);
            if (index <= 0) return new();
            return new() { field[index - 1] };
        }

        private List<CombatCard> GetAboveAllCard(APIContext ctx)
        {
            var caster = ctx.Caster;
            var source = ctx.SourceCard;
            if (caster == null || source == null) return new();
            var field = caster.GetField();
            int index = caster.GetIndexByCard(source);
            if (index <= 0) return new();
            return field.Take(index).ToList();
        }

        private List<CombatCard> GetBelowOneCard(APIContext ctx)
        {
            var caster = ctx.Caster;
            var source = ctx.SourceCard;
            if (caster == null || source == null) return new();
            var field = caster.GetField();
            int index = caster.GetIndexByCard(source);
            if (index < 0 || index >= field.Count - 1) return new();
            return new() { field[index + 1] };
        }

        private List<CombatCard> GetBelowAllCard(APIContext ctx)
        {
            var caster = ctx.Caster;
            var source = ctx.SourceCard;
            if (caster == null || source == null) return new();
            var field = caster.GetField();
            int index = caster.GetIndexByCard(source);
            if (index < 0 || index >= field.Count - 1) return new();
            return field.Skip(index + 1).ToList();
        }

        private List<CombatCard> GetAllCard(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return new();
            return caster.GetField();
        }

        private List<CombatCard> GetTargetAllCard(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return new();
            var target = caster.GetTarget();
            if (target == null) return new();
            return target.GetField();
        }

        private List<CombatCard> GetTargetRandomCard(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return new();
            var target = caster.GetTarget();
            if (target == null) return new();
            var cards = target.GetField();
            if (cards.Count == 0) return new();
            var idx = caster.Scene.Soul.Random(0, cards.Count);
            return new() { cards[idx] };
        }

        // ── Npc ──────────────────────────────────────────

        private List<CombatNpc> GetSelfNpc(APIContext ctx)
        {
            if (ctx.Caster == null) return new();
            return new() { ctx.Caster };
        }

        private List<CombatNpc> GetTargetNpc(APIContext ctx)
        {
            var caster = ctx.Caster;
            if (caster == null) return new();
            var target = caster.GetTarget();
            if (target == null) return new();
            return new() { target };
        }

        // ── 分发 ──────────────────────────────────────────

        public List<CombatCard> GetTargetCard(string key, APIContext ctx)
        {
            if (_cardFuncs.TryGetValue(key, out var func))
                return func(ctx);
            LogMgr.Instance.Warn("[APIDomainFunc] 未注册的 Card Domain: '{0}'", key);
            return new();
        }

        public List<CombatNpc> GetTargetNpc(string key, APIContext ctx)
        {
            if (_npcFuncs.TryGetValue(key, out var func))
                return func(ctx);
            LogMgr.Instance.Warn("[APIDomainFunc] 未注册的 Npc Domain: '{0}'", key);
            return new();
        }
    }
}
