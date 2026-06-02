using UnityWorld.Core;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    public  static partial class CombatBaseFunc
    {
        private static APIContext Keyword(string keyword,APIContext ctx)
        {
            var caster = ctx.Caster;
            var target = ctx.Get<CombatCard>("Target");
            var isKeyword = target.HasKeyword(keyword);
            ctx.Set<bool>("Ret",isKeyword);
            ctx.Set<bool>("Result", isKeyword);
            return ctx;
        }
        /// <summary>是法宝。</summary>
        [APIFunc("IsFabao",APIType.Condition, "判断目标卡牌是否为法宝", Scope.CombatCard, "Target:CombatCard", "Result bool")]
        public static APIContext IsFabao(APIContext ctx)
        {
            return Keyword("FaBao",ctx);
        }
        [APIFunc("IsFaShu",APIType.Condition, "判断目标卡牌是否为法术", Scope.CombatCard, "Target:CombatCard", "Result bool")]
        public static APIContext IsFaShu(APIContext ctx)
        {
            return Keyword("FaShu",ctx);
        }
        [APIFunc("IsGongFa",APIType.Condition, "判断目标卡牌是否为功法", Scope.CombatCard, "Target:CombatCard", "Result bool")]
        public static APIContext IsGongFa(APIContext ctx)
        {
            return Keyword("GongFa",ctx);
        }
        [APIFunc("IsItem",APIType.Condition, "判断目标卡牌是否为物品", Scope.CombatCard, "Target:CombatCard", "Result bool")]
        public static APIContext IsItem(APIContext ctx)
        {
            return Keyword("Item",ctx);
        }
        [APIFunc("IsEquip",APIType.Condition, "判断目标卡牌是否为装备", Scope.CombatCard, "Target:CombatCard", "Result bool")]
        public static APIContext IsEquip(APIContext ctx)
        {
            return Keyword("Equip",ctx);
        }
        
        [APIFunc("IsZhaoShi",APIType.Condition, "判断目标卡牌是否为招式", Scope.CombatCard, "Target:CombatCard", "Result bool")]
        public static APIContext IsZhaoShi(APIContext ctx)
        {
            return Keyword("ZhaoShi",ctx);
        }
    }
}
    