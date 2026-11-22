using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Game.Domain
{
    public  static partial class CombatBaseFunc
    {
        /// <summary>判断指定角色在实际对拼中获胜、落败或打平。</summary>
        [APIFunc("CheckContestOutcome", APIType.Condition, "判断目标角色在实际对拼中获胜、落败或打平", Scope.CombatCard,
            "Reference:CombatNpc", "Outcome:String", "Result:Bool")]
        public static APIContext CheckContestOutcome(APIContext ctx)
        {
            var reference = ctx.Get<CombatNpc>("Reference");
            var matches = ctx.EventName == "OnContestOverflow" && reference != null &&
                (ctx.GetStringValue("Outcome") switch
                {
                    "Win" => ReferenceEquals(reference, ctx.Get<CombatNpc>("Winner")),
                    "Lose" => ReferenceEquals(reference, ctx.Get<CombatNpc>("Loser")),
                    "Draw" => ctx.GetValue("IsDraw", false) &&
                        (ReferenceEquals(reference, ctx.Get<CombatNpc>("ContestA")) ||
                         ReferenceEquals(reference, ctx.Get<CombatNpc>("ContestB"))),
                    _ => false
                });
            ctx.Set("Result", matches);
            ctx.Set("Ret", matches);
            return ctx;
        }

        /// <summary>检查显式传入的卡是否具有弹药机制且余弹为零。</summary>
        [APIFunc("CheckAmmoExhausted", APIType.Condition, "指定卡的弹药已耗尽", Scope.CombatCard,
            "Reference:CombatCard", "Result:Bool")]
        public static APIContext CheckAmmoExhausted(APIContext ctx)
        {
            bool empty = ctx.Get<CombatCard>("Reference") is { IsAmount: true, CurrentAmount: 0 };
            ctx.Set("Result", empty);
            ctx.Set("Ret", empty);
            return ctx;
        }

        /// <summary>检查指定卡当前的关键词，接受 Mod 注册的词名。</summary>
        [APIFunc("HasKeyword", APIType.Condition, "指定卡有该关键词", Scope.CombatCard,
            "Reference:CombatCard", "Keyword:String", "Result:Bool")]
        public static APIContext HasKeyword(APIContext ctx)
        {
            bool matches = ctx.Get<CombatCard>("Reference")?.HasKeyword(ctx.GetStringValue("Keyword")) == true;
            ctx.Set("Result", matches);
            ctx.Set("Ret", matches);
            return ctx;
        }

        /// <summary>判断指定消耗卡的当前次数是否满足比较。</summary>
        [APIFunc("CheckConsumeCount", APIType.Condition, "按剩余次数判断消耗卡", Scope.CombatCard,
            "Reference:CombatCard", "Comparison:String", "Amount:Int", "Result:Bool")]
        public static APIContext CheckConsumeCount(APIContext ctx)
        {
            var card = ctx.Get<CombatCard>("Reference");
            var amount = ctx.GetValue("Amount", 0);
            var satisfied = card?.HasConsume == true && amount >= 0 && (ctx.GetStringValue("Comparison") switch
            {
                "Equal" => card.ConsumeStack == amount,
                "AtLeast" => card.ConsumeStack >= amount,
                "AtMost" => card.ConsumeStack <= amount,
                _ => false
            });
            ctx.Set("Result", satisfied);
            ctx.Set("Ret", satisfied);
            return ctx;
        }

        /// <summary>选择背包指定方向共边的卡，可供动作使用 Selected 作为目标范围。</summary>
        [APIFunc("SelectDirectionalCards", APIType.Condition, "选取指定方向共边的卡", Scope.CombatCard,
            "Direction:String", "Keyword:String", "Result:List<CombatCard>")]
        public static APIContext SelectDirectionalCards(APIContext ctx)
        {
            var selected = FindDirectionalCards(ctx);
            ctx.CardTargets = selected;
            ctx.Set("Result", selected.ToList());
            ctx.Set("Ret", selected.Count > 0);
            return ctx;
        }

        /// <summary>判断指定方向是否有一张符合关键词的共边卡。</summary>
        [APIFunc("HasDirectionalCard", APIType.Condition, "判断指定方向是否有匹配卡", Scope.CombatCard,
            "Direction:String", "Keyword:String", "Result:Bool")]
        public static APIContext HasDirectionalCard(APIContext ctx)
        {
            var found = FindDirectionalCards(ctx).Count > 0;
            ctx.Set("Result", found);
            ctx.Set("Ret", found);
            return ctx;
        }

        /// <summary>计算指定方向符合关键词的共边卡数，每张实例计一次。</summary>
        [APIFunc("CountDirectionalCards", APIType.Condition, "计算指定方向的匹配卡数", Scope.CombatCard,
            "Direction:String", "Keyword:String", "Result:Int")]
        public static APIContext CountDirectionalCards(APIContext ctx)
        {
            var count = FindDirectionalCards(ctx).Count;
            ctx.Set("Result", count);
            ctx.Set("Ret", count > 0);
            return ctx;
        }

        private static List<CombatCard> FindDirectionalCards(APIContext ctx)
            => APIDomainFunc.GetDirectionalCards(ctx.SourceCard, ctx.GetStringValue("Direction"), ctx.GetStringValue("Keyword", "Any"));

        /// <summary>筛选一次成功充入灵元的卡和元素。</summary>
        [APIFunc("CheckManaCharged", APIType.Condition, "筛选本次充入的元素、卡种和满灵", Scope.CombatCard,
            "WantedElement:String", "Keyword:String", "OnlyFull:Bool", "Result:Bool")]
        public static APIContext CheckManaCharged(APIContext ctx)
        {
            var card = ctx.SourceCard;
            string element = ctx.GetStringValue("WantedElement", "Any");
            string keyword = ctx.GetStringValue("Keyword", "Any");
            bool matches = ctx.EventName == "OnManaCharged" && card != null &&
                (element == "Any" || element == ctx.GetStringValue("ChargedElement")) &&
                (keyword == "Any" || card.HasKeyword(keyword)) &&
                (!ctx.GetValue("OnlyFull", false) || ctx.GetValue("IsFullyCharged", false));
            ctx.Set("Result", matches);
            ctx.Set("Ret", matches);
            return ctx;
        }

        /// <summary>比较一次成功支付的五行明细。</summary>
        [APIFunc("CheckManaUsed", APIType.Condition, "判断本次使用灵元的数量", Scope.CombatCard,
            "Measure:String", "AtLeast:Int", "Result:Bool")]
        public static APIContext CheckManaUsed(APIContext ctx)
        {
            string measure = ctx.GetStringValue("Measure");
            int atLeast = ctx.GetValue("AtLeast", 0);
            int amount = measure switch
            {
                "Total" or "Kinds" => ctx.GetValue(measure, 0),
                "Jin" or "Mu" or "Shui" or "Huo" or "Tu" => ctx.GetValue("Paid" + measure, 0),
                _ => -1
            };
            bool matches = ctx.EventName == "OnManaUsed" && atLeast > 0 && amount >= atLeast;
            ctx.Set("Result", matches);
            ctx.Set("Ret", matches);
            return ctx;
        }

        /// <summary>按指定五行状态和变化方向筛选已提交的层数变化。</summary>
        [APIFunc("CheckElementStackChange", APIType.Condition, "筛选具体五行状态的增减层", Scope.CombatCard,
            "StatusId:String", "Change:String", "Result:Bool")]
        public static APIContext CheckElementStackChange(APIContext ctx)
        {
            string id = ctx.GetStringValue("StatusId");
            long delta = ctx.GetValue("Delta", 0L);
            bool matches = ctx.EventName == "OnElementStackChanged" && CombatNpc.IsBaseElementStatus(id) &&
                id == ctx.GetStringValue("ChangedBuffId") && (ctx.GetStringValue("Change", "Any") switch
                {
                    "Increase" => delta > 0,
                    "Decrease" => delta < 0,
                    "Any" => delta != 0,
                    _ => false
                });
            ctx.Set("Result", matches);
            ctx.Set("Ret", matches);
            return ctx;
        }

        /// <summary>筛选伤势所属阵营与实际大小。</summary>
        [APIFunc("CheckWoundEvent", APIType.Condition, "筛选本次伤势的阵营和体量", Scope.CombatCard,
            "Reference:CombatNpc", "Side:String", "AtLeastSize:Int", "Result:Bool")]
        public static APIContext CheckWoundEvent(APIContext ctx)
        {
            var reference = ctx.Get<CombatNpc>("Reference");
            int minSize = ctx.GetValue("AtLeastSize", 1);
            bool matches = (ctx.EventName is "OnWoundGained" or "OnWoundHealed") && reference != null && ctx.Caster != null &&
                ctx.GetValue("WoundSize", 0) >= minSize && minSize > 0 && (ctx.GetStringValue("Side", "Any") switch
                {
                    "Ally" => reference.Team == ctx.Caster.Team,
                    "Enemy" => reference.Team != ctx.Caster.Team,
                    "Any" => true,
                    _ => false
                });
            ctx.Set("Result", matches);
            ctx.Set("Ret", matches);
            return ctx;
        }

        /// <summary>按耗尽原因筛选灵盾耗尽事件。</summary>
        [APIFunc("CheckShieldExhaustion", APIType.Condition, "筛选灵盾耗尽原因", Scope.CombatCard,
            "Cause:String", "Result:Bool")]
        public static APIContext CheckShieldExhaustion(APIContext ctx)
        {
            string cause = ctx.GetStringValue("Cause");
            bool matches = ctx.EventName == "OnShieldExhausted" && (cause == "Any" || cause == ctx.GetStringValue("Reason"));
            ctx.Set("Result", matches);
            ctx.Set("Ret", matches);
            return ctx;
        }

        /// <summary>按发动瞬间的占格判断事件来源是否在指定参考卡的某一方向。</summary>
        [APIFunc("WasCardUsedOnSide", APIType.Condition, "判断指定方向的卡是否实际发动", Scope.CombatCard,
            "Reference:CombatCard", "Direction:String", "Keyword:String", "Result:Bool")]
        public static APIContext WasCardUsedOnSide(APIContext ctx)
        {
            var source = ctx.SourceCard;
            var reference = ctx.Get<CombatCard>("Reference");
            var keyword = ctx.GetStringValue("Keyword", "Any");
            var placements = ctx.EventPlacements;
            var matches = ctx.EventName == "OnUse" && reference?.Owner != null && source != null && !ReferenceEquals(reference, source) &&
                ReferenceEquals(reference.Owner, source.Owner) && (keyword == "Any" || source.HasKeyword(keyword)) &&
                placements != null && placements.TryGetValue(reference.Id, out var referenceCells) &&
                placements.TryGetValue(source.Id, out var sourceCells) &&
                CombatBackpack.HasSharedEdge(referenceCells, sourceCells, ctx.GetStringValue("Direction"));
            ctx.Set("Result", matches);
            ctx.Set("Ret", matches);
            return ctx;
        }

        /// <summary>判断被移动卡是否新进入参照卡的指定方位。</summary>
        [APIFunc("WasCardMovedIntoSide", APIType.Condition, "指定方位由不共边变为共边", Scope.CombatCard,
            "Reference:CombatCard", "Direction:String", "Keyword:String", "Result:Bool")]
        public static APIContext WasCardMovedIntoSide(APIContext ctx)
        {
            var moved = ctx.Get<CombatCard>("MovedCard");
            var reference = ctx.Get<CombatCard>("Reference");
            var keyword = ctx.GetStringValue("Keyword", "Any");
            var direction = ctx.GetStringValue("Direction");
            var placements = ctx.EventPlacements;
            bool matches = false;
            if (ctx.EventName == "OnCardDisplaced" && moved?.Owner != null && reference != null && moved != reference &&
                ReferenceEquals(moved.Owner, reference.Owner) && (keyword == "Any" || moved.HasKeyword(keyword)) &&
                placements != null && placements.TryGetValue(reference.Id, out var referenceCells) &&
                placements.TryGetValue(moved.Id, out var newCells) && CombatBackpack.HasSharedEdge(referenceCells, newCells, direction))
            {
                int dx = ctx.GetValue("OldX", 0) - ctx.GetValue("NewX", 0);
                int dy = ctx.GetValue("OldY", 0) - ctx.GetValue("NewY", 0);
                var oldCells = newCells.Select(cell => new BackpackCell(cell.X + dx, cell.Y + dy)).ToArray();
                matches = !CombatBackpack.HasSharedEdge(referenceCells, oldCells, direction);
            }
            ctx.Set("Result", matches);
            ctx.Set("Ret", matches);
            return ctx;
        }

        /// <summary>判断一次位移事件中被移动的卡是否为指定实例。</summary>
        [APIFunc("WasCardDisplaced", APIType.Condition, "判断指定卡是否在这一事件中被位移", Scope.CombatCard,
            "Reference:CombatCard", "Result:Bool")]
        public static APIContext WasCardDisplaced(APIContext ctx)
        {
            var reference = ctx.Get<CombatCard>("Reference");
            var matches = ctx.EventName == "OnCardDisplaced" && reference != null &&
                ReferenceEquals(reference, ctx.Get<CombatCard>("MovedCard"));
            ctx.Set("Result", matches);
            ctx.Set("Ret", matches);
            return ctx;
        }

        [APIFunc("SelectCards", APIType.Condition, "按战斗 Domain 选卡", Scope.CombatCard, "Domain:String", "Result:List<CombatCard>")]
        public static APIContext SelectCards(APIContext ctx)
        {
            var cards = APIMgr.Instance.GetTargetCard(ctx.GetStringValue("Domain"), ctx);
            // 选中域供后续动作使用。Lua 仍可独立修改返回的副本。
            ctx.CardTargets = cards.ToList();
            ctx.Set("Result", cards.ToList());
            ctx.Set("Ret", cards.Count > 0);
            return ctx;
        }

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
        [APIFunc("IsFabao",APIType.Condition, "判断目标卡牌是否为法宝", Scope.CombatCard, "Target:CombatCard", "Result:Bool")]
        public static APIContext IsFabao(APIContext ctx)
        {
            return Keyword("FaBao",ctx);
        }
        [APIFunc("IsFaShu",APIType.Condition, "判断目标卡牌是否为法术", Scope.CombatCard, "Target:CombatCard", "Result:Bool")]
        public static APIContext IsFaShu(APIContext ctx)
        {
            return Keyword("FaShu",ctx);
        }
        [APIFunc("IsGongFa",APIType.Condition, "判断目标卡牌是否为功法", Scope.CombatCard, "Target:CombatCard", "Result:Bool")]
        public static APIContext IsGongFa(APIContext ctx)
        {
            return Keyword("GongFa",ctx);
        }
        [APIFunc("IsItem",APIType.Condition, "判断目标卡牌是否为物品", Scope.CombatCard, "Target:CombatCard", "Result:Bool")]
        public static APIContext IsItem(APIContext ctx)
        {
            return Keyword("Item",ctx);
        }
        [APIFunc("IsEquip",APIType.Condition, "判断目标卡牌是否为装备", Scope.CombatCard, "Target:CombatCard", "Result:Bool")]
        public static APIContext IsEquip(APIContext ctx)
        {
            return Keyword("Equip",ctx);
        }
        
        [APIFunc("IsZhaoShi",APIType.Condition, "判断目标卡牌是否为招式", Scope.CombatCard, "Target:CombatCard", "Result:Bool")]
        public static APIContext IsZhaoShi(APIContext ctx)
        {
            return Keyword("ZhaoShi",ctx);
        }
    }
}
