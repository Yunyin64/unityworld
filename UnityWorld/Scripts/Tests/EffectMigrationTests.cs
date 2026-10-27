using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests
{
    /// <summary>真实API/Lua包装与已迁移旧定义的效果回归。</summary>
    public static class EffectMigrationTests
    {
        private static int _count;
        private static void Check(bool condition, string message)
        {
            _count++;
            if (!condition) throw new InvalidOperationException("效果迁移断言失败：" + message);
        }
        private static Npc Npc()
        {
            var npc = BackpackModelTests.CreateNpc();
            npc.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "effect-test"));
            return npc;
        }
        private static CombatScene Scene(Npc a, Npc b)
        {
            var scene = new CombatScene();
            scene.Init(93471, new[] { (a, CombatTeam.TeamA), (b, CombatTeam.TeamB) }, 500);
            scene.PreStart(); scene.Start(); return scene;
        }
        private static APIContext Ctx(CombatNpc npc, string domain = "Self", CombatCard source = null)
        {
            var ctx = new APIContext { Caster = npc, Scene = npc.Scene, SourceCard = source };
            ctx.Set("Domain", domain); return ctx;
        }
        private static void Light(CombatNpc npc, CombatCard card)
        {
            foreach (var (element, count) in card.GetManaCost())
                for (int i = 0; i < count; i++)
                {
                    var cell = npc.Backpack.EmptyCells()[0];
                    npc.Backpack.PlaceMana(cell, element);
                    Check(npc.Backpack.TryAssign(cell, card), "真实元素点亮");
                }
        }
        public static void Run()
        {
            _count = 0;
            SpatialAndControl();
            ManaEffects();
            LuaEffectChain();
            Compatibility();
            LegacyDefinitions();
            Console.WriteLine($"EFFECT_MIGRATION_TESTS_OK assertions={_count}");
        }

        private static void SpatialAndControl()
        {
            var a = Npc(); var b = Npc();
            var source = BackpackModelTests.Place(a, "bp_focus", 3, 2);
            var top = BackpackModelTests.Place(a, "bp_focus", 4, 0);
            var crossing = BackpackModelTests.Place(a, "bp_charge", 5, 1);
            var wound = BackpackModelTests.Place(a, "bp_wound_small", 1, 0);
            var scene = Scene(a, b); var npc = scene.GetCombatants()[a.Id];
            var card = npc.GetField().Single(c => c.Id == source.Id);
            var ctx = Ctx(npc, "AboveAll", card);
            var selected = APIMgr.Instance.GetTargetCard("AboveAll", ctx).Select(c => c.Id).ToArray();
            Check(selected.Contains(top.Id) && selected.Contains(wound.Id) && selected.Contains(npc.Backpack.MainCardId), "完整上方包含异列卡、伤口和主功法");
            Check(!selected.Contains(crossing.Id) && !selected.Contains(source.Id), "跨入来源行的目标排除");
            npc.GetField().Reverse();
            Check(selected.SequenceEqual(APIMgr.Instance.GetTargetCard("AboveAll", ctx).Select(c => c.Id)), "AboveAll不依赖Field列表顺序");

            ctx = Ctx(npc, "All", card); ctx.Set("DurationTicks", 20);
            CombatBaseFunc.Haste(ctx); Check(npc.Backpack.HasteTicks == 20, "多卡选择只对角色加速一次");
            CombatBaseFunc.Haste(ctx); Check(npc.Backpack.HasteTicks == 40, "同类加时");
            CombatBaseFunc.Slow(ctx); Check(npc.Backpack.SlowTicks == 20, "减速独立时长");
            ctx.Set("ReduceTick", 10); CombatBaseFunc.Charge(ctx);
            Check(npc.Backpack.ScanProgress == 10m && npc.Backpack.HasteTicks == 40 && npc.Backpack.SlowTicks == 20, "整包充能一次且不扣效果时间");
            Check(scene.GetCombatants()[b.Id].Backpack.ScanProgress == 0, "充能不推进对手");

            foreach (var candidate in npc.GetField().Where(c => c.Id != top.Id)) candidate.Freeze();
            ctx = Ctx(npc, "Random", card); CombatBaseFunc.Freeze(ctx);
            Check(npc.GetField().Single(c => c.Id == top.Id).IsFrozen && ctx.CardTargets.Count == 1, "随机冻结优先范围内唯一未冻结目标");
            CombatBaseFunc.Freeze(ctx); Check(ctx.CardTargets.Count == 0, "全冻结没有新增效果");
            Check(npc.GetField().All(c => c.Ticks["CD"] == 0), "控制API不推进旧单卡CD");
            scene.Cleanup();

            a = Npc(); b = Npc(); scene = Scene(a, b); npc = scene.GetCombatants()[a.Id];
            npc.AddModifier("Element_Buff_Huo", 1);
            npc.AddModifier("Element_Debuff_Shui", 2);
            npc.DoManaDraw();
            Check(npc.Backpack.HasteTicks == 10 && npc.Backpack.SlowTicks == 20, "真实元素Buff每层折算10个真实Tick并作用于整包");
            scene.Tick();
            Check(npc.Backpack.HasteTicks == 9 && npc.Backpack.SlowTicks == 19 && npc.Backpack.ScanProgress == 1m, "真实时间仅扣一次，双效果倍率抵消");
            Console.WriteLine("ELEMENT_TIMELINE_OK draw:haste=10,slow=20 tick1:haste=9,slow=19,progress=1");
            scene.Cleanup();
        }

        private static void ManaEffects()
        {
            var a = Npc(); var b = Npc(); var source = BackpackModelTests.Place(a, "bp_charge", 2, 0);
            var scene = Scene(a, b); var npc = scene.GetCombatants()[a.Id];
            var card = npc.GetField().Single(c => c.Id == source.Id); Light(npc, card);
            using var eventLua = new NLua.Lua();
            card.env = (NLua.LuaTable)eventLua.DoString("return {OnMPChanged=function(self,ctx) self.changes=(self.changes or 0)+1 end}")[0];
            card.ScanLuaHooks();
            var empty = npc.Backpack.EmptyCells();
            npc.Backpack.PlaceMana(empty[0], ElementType.Jin); npc.Backpack.PlaceMana(empty[1], ElementType.Huo);
            var ctx = Ctx(npc); ctx.Set("Element", "None"); ctx.Set("MaxAmount", 3);
            CombatBaseFunc.Convert(ctx); Check(npc.Backpack.Mana.Count == 2, "MP满回收0个实体");
            npc.ReduceMP(1); var before = npc.GetMp(); CombatBaseFunc.Convert(ctx);
            Check(npc.Backpack.Mana.Count == 1 && npc.GetMp() == before + 1, "仅差1MP只回收一个");
            Check(System.Convert.ToInt32(card.env["changes"]) == 2, "一次回收只发送一次MP变化，满MP回收不发送");
            Check(npc.Backpack.LitCount(card.Id) == 3, "回收保护卡上点亮");
            ctx.Set("MaxAmount", -1); CombatBaseFunc.Convert(ctx); Check(npc.Backpack.Mana.Count == 1, "非正请求不删除");
            ctx.Set("Amount", 3); CombatBaseFunc.ReduceMana(ctx);
            Check(npc.Backpack.Mana.Count == 0 && npc.Backpack.LitCount(card.Id) == 3, "减灵部分成功且不影响卡上资源");
            npc.ReduceMP(3);
            empty = npc.Backpack.EmptyCells();
            npc.Backpack.PlaceMana(empty[0], ElementType.Jin); npc.Backpack.PlaceMana(empty[1], ElementType.Huo);
            ctx.Set("Element", "Jin"); ctx.Set("MaxAmount", 5); before = npc.GetMp();
            CombatBaseFunc.Convert(ctx);
            Check(npc.GetMp() == before + 1 && npc.Backpack.Mana.Count == 1 && npc.Backpack.Mana.Values.Single().Equals(ElementType.Huo), "元素过滤与候选不足只回收实际数量");
            var mp = npc.GetMp(); npc.RecoverMP(-2); npc.ReduceMP(-2); Check(npc.GetMp() == mp, "非正MP操作无副作用");
            scene.Cleanup();

            // Same seed and entity order replay exactly, regardless of dictionary insertion order.
            a = Npc(); b = Npc(); scene = Scene(a, b);
            npc = scene.GetCombatants()[a.Id]; var other = scene.GetCombatants()[b.Id];
            var positions = npc.Backpack.EmptyCells().Take(5).ToArray();
            foreach (var cell in positions) npc.Backpack.PlaceMana(cell, cell == positions[0] ? ElementType.Huo : ElementType.Jin);
            foreach (var cell in positions.Reverse()) other.Backpack.PlaceMana(cell, cell == positions[0] ? ElementType.Huo : ElementType.Jin);
            var soul1 = new SoulData(734); var soul2 = new SoulData(734);
            var r1 = npc.Backpack.RemoveMana(4, null, soul1); var r2 = other.Backpack.RemoveMana(4, null, soul2);
            Check(r1.SequenceEqual(r2) && r1.Select(p => p.Key).Distinct().Count() == 4, "实体随机不放回且稳定候选可重放");
            scene.Cleanup();
        }

        private static void LuaEffectChain()
        {
            var a = Npc(); var b = Npc(); var source = BackpackModelTests.Place(a, "bp_focus", 1, 0);
            var scene = Scene(a, b); var npc = scene.GetCombatants()[a.Id];
            var card = npc.GetField().Single(c => c.Id == source.Id); Light(npc, card);
            npc.Backpack.PlaceMana(new(4, 0), ElementType.Jin);
            using var lua = new NLua.Lua(); lua.LoadCLRPackage(); lua["API"] = APIMgr.Instance;
            var scripts = Path.Combine(Path.GetDirectoryName(CardDefineMgr.Instance.DataDir)!, "LuaScripts");
            lua.DoString(File.ReadAllText(Path.Combine(scripts, "CombatNpc_Action.lua")));
            lua.DoString("function chain(ctx) ReduceMana(ctx, 'Self', 'None', 3); Draw(ctx, 'Self', 1) end");
            var apply = (NLua.LuaTable)lua.DoString("return {Apply=function(self,ctx) ReduceMana(ctx, 'Self', 'None', 3) end}")[0];
            card.env = apply; card.ScanLuaHooks();
            while (scene.CurrentTick < 10) scene.Tick();
            Check(card.UseCount == 1 && npc.Backpack.Mana.Count == 0 && npc.Backpack.LitCount(card.Id) == 0, "左侧真实Lua/API减灵使右侧实体不再被扫描分配");
            var free = npc.Backpack.EmptyCells()[0]; npc.Backpack.PlaceMana(free, ElementType.Jin);
            var mp = npc.GetMp(); lua.GetFunction("chain").Call(Ctx(npc));
            Check(npc.Backpack.Mana.Count == 1 && npc.GetMp() == mp - 1, "效果链删除立即腾格供后续Draw使用");
            Check(npc.Backpack.Row == 1 && card.UseCount == 1, "已扫格不倒退重扫");
            scene.Cleanup();
        }

        private static void Compatibility()
        {
            var a = Npc(); var b = Npc(); var source = BackpackModelTests.Place(a, "bp_attack", 1, 0);
            var scene = Scene(a, b); var npc = scene.GetCombatants()[a.Id];
            var card = npc.GetField().Single(c => c.Id == source.Id); Light(npc, card);
            npc.Backpack.PlaceMana(new(5, 4), ElementType.Huo);
            while (scene.CurrentTick < 10) scene.Tick();
            var pending = npc.PendingSlot;
            Check(pending != null, "拒绝操作测试先建立真实待发");
            var ctx = Ctx(npc, "Self", card);
            var snapshot = string.Join(";", npc.Backpack.Placements.Select(p => $"{p.Key}:{string.Join(',', p.Value)}"));
            var sp = npc.GetSp(); var mp = npc.GetMp();
            foreach (var stat in new[] { "Min", "Max", "ManaAdj_Jin" })
            {
                ctx.Set("StatId", stat); var rejected = false;
                try { CombatBaseFunc.AddCardStatBuff(ctx); } catch (NotSupportedException e) { rejected = e.Message.Contains("Deprecated"); }
                Check(rejected, "废弃属性运行防线:" + stat);
            }
            foreach (var action in new Func<APIContext, APIContext>[] { CombatBaseFunc.Deploy, CombatBaseFunc.Recall, CombatBaseFunc.Displace })
            {
                var rejected = false; try { action(ctx); } catch (NotSupportedException e) { rejected = e.Message.Contains("Pending"); }
                Check(rejected, "待定神修操作明确拒绝");
            }
            Check(snapshot == string.Join(";", npc.Backpack.Placements.Select(p => $"{p.Key}:{string.Join(',', p.Value)}")) && npc.GetSp() == sp && npc.GetMp() == mp && ReferenceEquals(npc.PendingSlot, pending) && npc.Backpack.Mana.Count == 1 && npc.Backpack.Mana[new(5, 4)].Equals(ElementType.Huo), "拒绝后位置SP资源待发完全不变");
            scene.Cleanup();
        }

        private static void LegacyDefinitions()
        {
            var all = CardDefineMgr.Instance.GetAll().Where(c => !c.ID.StartsWith("bp_")).ToArray();
            Check(all.All(c => c.Size > 0), "所有旧Size0定义已明确改1");
            foreach (var definition in all.Where(c => c.CompatibilityStatus == "Migrated"))
            {
                if (definition.ID == "card_fabao_jian") continue; // Equipment and real event assertions live in ActivationEventTests.
                var a = Npc(); var b = Npc(); var source = BackpackModelTests.Place(a, definition.ID, 1, 0);
                var scene = Scene(a, b); var npc = scene.GetCombatants()[a.Id];
                var card = npc.GetField().Single(c => c.Id == source.Id);
                if (card.BaseData.HasActiveAbility)
                {
                    Check(card.GetManaCost().Values.Sum() == card.GetSize(), definition.ID + "资源总量等于Size");
                    Light(npc, card); while (scene.CurrentTick < 50 && !scene.IsFinished) scene.Tick();
                    Check(card.UseCount == 1 && card.ApplyCount == 1, definition.ID + "真实定义Lua一次Use/Apply");
                }
                else Check(!npc.Backpack.CanAccept(card, ElementType.Jin) && card.UseCount == 0, definition.ID + "纯被动不被强制收灵");
                Console.WriteLine($"MIGRATED_CARD_OK id={definition.ID} size={card.GetSize()} use={card.UseCount} apply={card.ApplyCount}");
                scene.Cleanup();
            }
        }
    }
}
