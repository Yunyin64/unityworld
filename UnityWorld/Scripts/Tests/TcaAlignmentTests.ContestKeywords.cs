using NLua;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests;

public static partial class TcaAlignmentTests
{
    private static void VerifyContestKeywords(Lua lua)
    {
        var a = BackpackModelTests.CreateNpc();
        var b = BackpackModelTests.CreateNpc();
        var witness = BackpackModelTests.Place(a, "bp_focus", 1, 0);
        var scene = new CombatScene();
        scene.Init(7451, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 100);
        try
        {
            scene.PreStart();
            scene.Start();
            var owner = scene.GetCombatants()[a.Id];
            var opponent = scene.GetCombatants()[b.Id];
            var source = owner.GetField().Single(card => card.Id != witness.Id);
            var rival = opponent.GetField().Single();
            var bystander = owner.GetField().Single(card => card.Id == witness.Id);

            const string probeId = "TestContestPairSources";
            var probe = (LuaTable)lua.DoString("""
                local kw = { Calls = 0 }
                function kw.OnResolveContest(card, ctx)
                    if ctx.Caster == card.Owner and ctx.SourceCard == card then
                        kw.Calls = kw.Calls + (ctx:GetObject("IsWinner") and 1 or 2)
                        ctx:Set("BaseDamage", ctx:GetObject("BaseDamage") + 1)
                    end
                end
                return kw
                """)[0];
            LuaMgr.Instance.RegisterKeyword(probeId, probe);

            void SetKeyword(CombatCard card, string keyword)
            {
                card.GetKeywords().RemoveAll(k => k is "Zhan" or "Ci" or "Da" or "Block" or "Dodge" or probeId);
                if (keyword != null) card.GetKeywords().Add(keyword);
            }

            int Pair(ContestType left, int leftValue, ContestType right, int rightValue)
            {
                var hp = opponent.GetHp();
                owner.ResolveContest(new ContestData { SourceCard = source, ContestType = left, ContestValue = leftValue },
                    new ContestData { SourceCard = rival, ContestType = right, ContestValue = rightValue });
                owner.DealDamage();
                opponent.DealDamage();
                int damage = hp - opponent.GetHp();
                opponent.ApplyHeal(damage);
                return damage;
            }

            SetKeyword(source, probeId);
            SetKeyword(rival, probeId);
            SetKeyword(bystander, probeId);
            opponent.ChangeShield(1);
            Check(Pair(ContestType.Zhan, 6, ContestType.Ci, 4) == 3 && Convert.ToInt32(probe["Calls"]) == 3,
                "配对两张来源卡各执行一次，基础2加2，再由伤害管线吸收1");
            Check(Pair(ContestType.Zhan, 4, ContestType.Ci, 4) == 0 && Convert.ToInt32(probe["Calls"]) == 3,
                "平局不执行胜负关键词");
            SetKeyword(bystander, null);
            SetKeyword(rival, null);

            foreach (var type in new[] { ContestType.Zhan, ContestType.Ci, ContestType.Da })
            {
                SetKeyword(source, type.ToString());
                Check(Pair(type, 6, type, 4) == 6, $"同类 {type} 由自身关键词通吃");
                Check(Pair(type, 6, ContestType.SheJi, 4) == 2, $"{type} 对射击取差值");
            }
            SetKeyword(source, "Zhan");
            Check(Pair(ContestType.Ci, 6, ContestType.Ci, 4) == 2, "词与实际攻击类型不符时取差值");
            Check(Pair(ContestType.SheJi, 6, ContestType.Zhan, 4) == 2
                && Pair(ContestType.SheJi, 6, ContestType.SheJi, 4) == 2, "射击对攻击取差值");
            SetKeyword(rival, "Dodge");
            Check(Pair(ContestType.SheJi, 6, ContestType.Dodge, 4) == 6, "射击胜过闪时由败方词改为全额");
            SetKeyword(rival, "Block");
            Check(Pair(ContestType.Zhan, 6, ContestType.Block, 4) == 2
                && Pair(ContestType.Zhan, 6, ContestType.Shield, 4) == 2, "格和挡落败取差值");

            const string counter = "Attack_Changed_Once";
            SetKeyword(source, "Block");
            Pair(ContestType.Block, 110, ContestType.Zhan, 6);
            Check(owner.GetAllModifiers().Single(mod => mod.DefineId == counter).CurrentStack == 99
                && opponent.GetAllModifiers().All(mod => mod.DefineId != counter), "格给自己施加现有单次强化，沿用99层上限");
            int NextValue(ContestType type)
            {
                var ctx = new APIContext { Caster = owner, SourceCard = source, Scene = scene };
                var contest = new ContestData { SourceCard = source, ContestType = type, ContestValue = 4 };
                ctx.Set("ContestData", contest);
                owner.ModifyHook("Contest", ctx);
                return contest.FinalValue;
            }
            Check(NextValue(ContestType.Block) == 4 && NextValue(ContestType.Ci) == 103 && NextValue(ContestType.Zhan) == 4,
                "格的奖励只由自己下一道攻击消费");
            Pair(ContestType.Block, 8, ContestType.Shield, 6);
            Pair(ContestType.Block, 6, ContestType.Zhan, 6);
            owner.Straight(new ContestData { SourceCard = source, ContestType = ContestType.Block, ContestValue = 8 });
            Check(owner.GetAllModifiers().All(mod => mod.DefineId != counter), "格对防御、平局和单方待发无奖励");

            SetKeyword(source, "Dodge");
            var progress = owner.Backpack.ScanProgress;
            var otherProgress = opponent.Backpack.ScanProgress;
            Pair(ContestType.Dodge, 8, ContestType.Zhan, 6);
            Pair(ContestType.Dodge, 8, ContestType.Shield, 6);
            Pair(ContestType.Dodge, 6, ContestType.Zhan, 6);
            owner.Straight(new ContestData { SourceCard = source, ContestType = ContestType.Dodge, ContestValue = 8 });
            Pair(ContestType.Shield, 8, ContestType.Zhan, 6);
            Check(owner.Backpack.ScanProgress == progress + 2 && opponent.Backpack.ScanProgress == otherProgress,
                "只有闪赢敌方攻击才给自己的背包充能");
        }
        finally { scene.Cleanup(); }
    }
}
