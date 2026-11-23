using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests;

public static partial class TcaAlignmentTests
{
    private static void VerifySpatialEffects(Lua lua)
    {
        var a = BackpackModelTests.CreateNpc();
        // 专用几何回归直接在实例上构造缺口，正式功法始终给完整矩形。
        NpcMgr.Instance.BackpackSystem.GetData(a.Id).ValidCells.Remove(new(0, 4));
        var b = BackpackModelTests.CreateNpc();
        a.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-test"));
        b.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-test"));
        a.Stats.SetBase("ShenShi", 11);

        Card PlaceWide(string label, int x, int y)
        {
            var wide = CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = label, Size = 2, Keywords = ["Passive"] });
            a.GainCard(wide);
            a.PlaceBackpackCardInternal(wide.Id, x, y);
            return wide;
        }

        var anchor = PlaceWide("tca-grid-anchor", 1, 1);
        var above = PlaceWide("tca-grid-above", 1, 0);
        var right = CardMgr.Instance.InstantiateFromDefine(new CardDefine
        {
            ID = "bp_focus", Size = 2, Keywords = ["FaShu"], ManaCost = new() { ["Jin"] = 2 }
        });
        a.GainCard(right);
        a.PlaceBackpackCardInternal(right.Id, 3, 1);
        var corner = BackpackModelTests.Place(a, "bp_attack", 3, 0);
        var slider = PlaceWide("tca-grid-slider", 0, 3);
        var blocker = BackpackModelTests.Place(a, "bp_attack", 5, 3);
        var enemySlider = BackpackModelTests.Place(b, "bp_focus", 0, 1);
        var scene = new CombatScene();
        scene.Init(7355, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 200);
        var movedEvents = new List<APIContext>();
        var listener = new DelegateEventListener((_, _, args) => movedEvents.Add((APIContext)args));
        EventMgr.Instance.RegisterEvent("tca-grid-move", "OnCardDisplaced", ScopeKey.Global, listener);
        var otherCardMoves = new List<APIContext>();
        var otherCardListener = new DelegateEventListener((_, _, args) => otherCardMoves.Add((APIContext)args));
        EventMgr.Instance.RegisterEvent("tca-grid-move-other", "OnDisplaceOtherCard", ScopeKey.Global, otherCardListener);
        try
        {
            scene.PreStart();
            scene.Start();
            var npc = scene.GetCombatants()[a.Id];
            CombatCard CardInBattle(Card card) => npc.GetField().Single(current => current.Id == card.Id);
            var reference = CardInBattle(anchor);
            var source = CardInBattle(right);
            var wide = CardInBattle(slider);
            var stop = CardInBattle(blocker);
            var ctx = new APIContext { Caster = npc, SourceCard = reference, Scene = scene };
            using var select = lua.GetFunction("SelectDirectionalCards");
            using var count = lua.GetFunction("CountDirectionalCards");
            using var has = lua.GetFunction("HasDirectionalCard");
            using var slide = lua.GetFunction("SlideCard");
            using var wasMoved = lua.GetFunction("WasCardDisplaced");
            using var wasUsed = lua.GetFunction("WasCardUsedOnSide");
            Check(((List<CombatCard>)select.Call(ctx, "Up", "Any")[0]).SequenceEqual([CardInBattle(above)]),
                "多格共边只选一次，排除对角卡");
            Check(APIMgr.Instance.GetTargetCard("Selected", ctx).SequenceEqual([CardInBattle(above)]),
                "空间选中结果可交给现有动作使用");
            Check(System.Convert.ToInt32(count.Call(ctx, "Neighbor", "Any")[0]) == 2
                && (bool)has.Call(ctx, "Right", "FaShu")[0]
                && !(bool)has.Call(ctx, "Right", "FaBao")[0], "空间计数与存在复用方向及关键词条件");
            Check(System.Convert.ToInt32(count.Call(new APIContext(), "Neighbor", "Any")[0]) == 0, "无参照卡时空间条件为空");

            var sp = npc.GetSp();
            npc.Backpack.PlaceMana(new(2, 3), ElementType.Jin);
            npc.Backpack.PlaceMana(new(3, 3), ElementType.Mu);
            ctx.SourceCard = wide;
            slide.Call(ctx, "Self", "First");
            Check(!ctx.GetValue("Ret", true) && movedEvents.Count == 0 &&
                !npc.SlideCombatCard(wide, CombatCardSlideType.None), "旧列表位置和空方向都不会移动卡牌");
            slide.Call(ctx, "Self", CombatCardSlideType.Right);
            Check(npc.Backpack.GetCardPosition(wide.Id) == new BackpackPosition(4, 4) &&
                npc.Backpack.Mana.ContainsKey(new(2, 3)) && !npc.Backpack.Mana.ContainsKey(new(3, 3)) &&
                npc.Backpack.GetCardCells(wide.Id).SequenceEqual([new BackpackCell(3, 3), new BackpackCell(4, 3)])
                && ctx.GetValue("MovedCount", 0) == 1 && npc.GetSp() == sp,
                "整体滑到阻挡前，经过的灵元保留、终点覆盖让位");
            var moved = movedEvents.Single();
            Check(moved.GetStringValue("Direction") == "Right" &&
                (bool)wasMoved.Call(moved, wide)[0] && !(bool)wasMoved.Call(moved, stop)[0]
                && moved.GetValue("OldX", 0) == 1 && moved.GetValue("NewX", 0) == 4, "成功事件提供被位移卡与实际坐标");
            Check(otherCardMoves.Count == 0, "移动自己的牌只发被位移事件");
            slide.Call(ctx, "Self", "Right");
            ctx.SourceCard = npc.GetField().Single(card => card.Id == npc.Backpack.MainCardId);
            slide.Call(ctx, "Self", "Right");
            npc.AddContestData(new ContestData { SourceCard = stop, ContestValue = 3 });
            ctx.SourceCard = stop;
            slide.Call(ctx, "Self", "Up");
            Check(movedEvents.Count == 1 && npc.PendingSlot?.SourceCard == stop && !ctx.GetValue("Ret", true),
                "有障碍、固定卡和待发卡不移动、不发事件，动作报告失败");

            // 使用的卡在自身 Apply 中离开右邻，OnUse 仍按提交时的关系筛选。
            source.env = (LuaTable)lua.DoString("return {Apply = function(self, ctx) SlideCard(ctx, 'Self', 'Right') end}")[0];
            source.ScanLuaHooks();
            reference.env = (LuaTable)lua.DoString("""
                return {OnUse = function(self, ctx)
                    if WasCardUsedOnSide(ctx, self.m_Self, 'Right', 'FaShu') then
                        GainShield(ctx, 'Self', 2)
                    end
                end}
                """)[0];
            reference.env["m_Self"] = reference;
            reference.ScanLuaHooks();
            var useChecks = new List<bool>();
            var usedItself = false;
            var onUse = new DelegateEventListener((_, _, args) =>
            {
                var context = (APIContext)args;
                if (context.SourceCard == source)
                {
                    useChecks.Add((bool)wasUsed.Call(context, reference, "Right", "FaShu")[0]);
                    usedItself = (bool)wasUsed.Call(context, source, "Right", "Any")[0];
                }
            });
            EventMgr.Instance.RegisterEvent("tca-grid-use", "OnUse", ScopeKey.Global, onUse);
            try
            {
                for (int i = 0; i < source.GetSize(); i++)
                {
                    var cell = npc.Backpack.EmptyCells()[0];
                    npc.Backpack.PlaceMana(cell, ElementType.Jin);
                    Check(npc.Backpack.TryAssign(cell, source), "点亮实测位置联动卡");
                }
                for (int i = 0; i < 20; i++) scene.Tick();
                Check(useChecks.SequenceEqual([true]) && npc.Backpack.GetCardPosition(source.Id) == new BackpackPosition(5, 2)
                    && movedEvents.Count == 2, "使用事件读原共边，位移事件只发生一次且无需追扫");
                Check(!usedItself, "横卡的内部共边不能把自己的使用算作邻卡");
                ctx.SourceCard = reference;
                Check(!(bool)has.Call(ctx, "Right", "Any")[0] && CardInBattle(corner).UseCount == 0,
                    "后续查询读提交后的布局，单纯位置变化不触发使用");
                Check(npc.ShieldValue == 2, "真实 Lua 组合右邻发动、相对方位条件和获得灵盾");
                for (int i = 20; i < 50; i++) scene.Tick();
                ctx.SourceCard = wide;
                Check(npc.SlideCombatCard(wide, CombatCardSlideType.Down), "C# 方向枚举立即提交背包滑移");
                slide.Call(ctx, "Self", "Left");
                Check(!npc.Backpack.IsValid(new(0, 4)) && npc.Backpack.GetCardPosition(wide.Id) == new BackpackPosition(2, 5),
                    "向下遇边界停住，向左滑到无效格之前");
                for (int i = 50; i < 60; i++) scene.Tick();
                ctx.SourceCard = source;
                slide.Call(ctx, "Self", "Up");
                Check(npc.Backpack.GetCardPosition(source.Id) == new BackpackPosition(5, 1) &&
                    !npc.Backpack.IsCardCompleteAt(source.Id, new(5, 0)) && source.UseCount == 1,
                    "向上进入已扫行，不补扫、也不能再次获得本轮发动");

                var enemy = scene.GetCombatants()[b.Id];
                var beforeRandom = movedEvents.Count;
                slide.Call(ctx, "TargetRandom", "Right");
                Check(ctx.GetValue("MovedCount", 0) == 1 && movedEvents.Count == beforeRandom + 1 &&
                    enemy.Backpack.GetCardPosition(enemySlider.Id) == new BackpackPosition(6, 2),
                    "随机滑移找到实际可移动目标，跳过敌方固定主功法");
                var displaced = movedEvents[^1];
                var displacedCard = enemy.GetField().Single(card => card.Id == enemySlider.Id);
                Check(displaced.Caster == enemy && displaced.SourceCard == displacedCard &&
                    displaced.Get<CombatCard>("MovedCard") == displacedCard,
                    "跨阵营滑移的被位移事件报告被移动牌和它的主人");
                var displacedOther = otherCardMoves.Single();
                Check(displacedOther.Caster == npc && displacedOther.SourceCard == source &&
                    displacedOther.Get<CombatCard>("MovedCard") == displacedCard &&
                    displacedOther.GetValue("NewX", 0) == displaced.GetValue("NewX", -1),
                    "移动他人卡牌事件报告位移发起者，同时保留相同的目标和位移坐标");
                using var gainShield = lua.GetFunction("GainShield");
                var ownShield = npc.ShieldValue;
                var enemyShield = enemy.ShieldValue;
                gainShield.Call(displaced, "Self", 2);
                Check(npc.ShieldValue == ownShield && enemy.ShieldValue == enemyShield + 2,
                    "被位移事件内的 Self 灵盾给被移动牌的主人");
                gainShield.Call(displacedOther, "Self", 1);
                Check(npc.ShieldValue == ownShield + 1 && enemy.ShieldValue == enemyShield + 2,
                    "移动他人卡牌事件内的 Self 灵盾给位移发起者");
                slide.Call(ctx, "TargetRandom", "Right");
                Check(ctx.GetValue("MovedCount", 1) == 0 && movedEvents.Count == beforeRandom + 1 && otherCardMoves.Count == 1,
                    "固定卡和贴边卡都不可滑时，随机滑移不产生任一位移事件");
                slide.Call(ctx, "TargetRandom", "Random");
                Check(ctx.GetValue("MovedCount", 0) == 1 && movedEvents.Count == beforeRandom + 2 && otherCardMoves.Count == 2 &&
                    movedEvents[^1].GetStringValue("Direction") is "Up" or "Down" or "Left" &&
                    movedEvents[^1].GetStringValue("Direction") == otherCardMoves[^1].GetStringValue("Direction"),
                    "随机敌牌与随机方向可组合，双方事件携带同一个实际方向");
            }
            finally
            {
                EventMgr.Instance.RemoveEvent("OnUse", ScopeKey.Global, onUse);
            }
        }
        finally
        {
            EventMgr.Instance.RemoveEvent("OnCardDisplaced", ScopeKey.Global, listener);
            EventMgr.Instance.RemoveEvent("OnDisplaceOtherCard", ScopeKey.Global, otherCardListener);
            scene.Cleanup();
        }
    }

    private static void VerifyRandomSlide(Lua lua)
    {
        // 首行的横卡左侧是主功法。右侧和第二格下方各放一张阻挡牌，逐一移除以开放方向。
        var owner = BackpackModelTests.CreateNpc();
        var wide = BackpackModelTests.Place(owner, "bp_wound_wide", 1, 0);
        var rightBlock = BackpackModelTests.Place(owner, "bp_focus", 3, 0);
        var downBlock = BackpackModelTests.Place(owner, "bp_focus", 2, 1);
        BackpackModelTests.Place(owner, "bp_focus", 2, 2);
        var scene = new CombatScene();
        scene.Init(7355, [(owner, CombatTeam.TeamA), (BackpackModelTests.CreateNpc(), CombatTeam.TeamB)]);
        var moves = new List<APIContext>();
        var listener = new DelegateEventListener((_, _, args) => moves.Add((APIContext)args));
        EventMgr.Instance.RegisterEvent("tca-random-slide", "OnCardDisplaced", ScopeKey.Global, listener);
        try
        {
            scene.PreStart();
            scene.Start();
            var npc = scene.GetCombatants()[owner.Id];
            CombatCard Find(Card card) => npc.GetField().Single(c => c.Id == card.Id);
            var card = Find(wide);
            var ctx = new APIContext { Caster = npc, SourceCard = card, Scene = scene };
            using var slide = lua.GetFunction("SlideCard");
            slide.Call(ctx, "Self", "Random");
            Check(!ctx.GetValue("Ret", true) && moves.Count == 0 &&
                npc.Backpack.GetCardPosition(card.Id) == new BackpackPosition(2, 1), "整张横卡四向受阻时随机滑移失败且不发事件");

            npc.RemoveCombatCard(Find(rightBlock));
            npc.DealFieldChange();
            slide.Call(ctx, "Self", "Random");
            Check(ctx.GetValue("MovedCount", 0) == 1 && moves.Single().GetStringValue("Direction") == "Right" &&
                npc.Backpack.GetCardPosition(card.Id) == new BackpackPosition(5, 1), "只有右方可走时一定向右滑到头");
            npc.SlideCombatCard(card, CombatCardSlideType.Left);

            npc.RemoveCombatCard(Find(downBlock));
            npc.DealFieldChange();
            var directions = new HashSet<string>();
            for (int i = 0; i < 24; i++)
            {
                moves.Clear();
                slide.Call(ctx, "Self", "Random");
                var direction = moves.Single().GetStringValue("Direction");
                var position = npc.Backpack.GetCardPosition(card.Id);
                Check((direction == "Right" && position == new BackpackPosition(5, 1)) ||
                    (direction == "Down" && position == new BackpackPosition(2, 2)), "随机滑移的事件方向和完整卡形终点一致");
                directions.Add(direction);
                npc.SlideCombatCard(card, direction == "Right" ? CombatCardSlideType.Left : CombatCardSlideType.Up);
            }
            Check(directions.SetEquals(["Right", "Down"]), "固定种子下既能抽到三格的右方，也能抽到一格的下方");
        }
        finally
        {
            EventMgr.Instance.RemoveEvent("OnCardDisplaced", ScopeKey.Global, listener);
            scene.Cleanup();
        }
    }
}
