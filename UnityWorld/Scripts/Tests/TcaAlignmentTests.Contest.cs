using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests;

public static partial class TcaAlignmentTests
{
    private static void VerifyResourceSettlement(Lua lua, CombatNpc owner, CombatNpc opponent)
    {
        var ctx = new APIContext { Caster = owner, Scene = owner.Scene };
        using var heal = lua.GetFunction("Heal");
        var maxHp = owner.GetHp();
        owner.ApplyDamage(new DamageInfo(opponent, null, owner, 2));
        var healedHp = new List<int>();
        var listener = new DelegateEventListener((_, _, args) =>
        {
            var healed = (APIContext)args;
            healedHp.Add(healed.Caster.GetHp());
            Check(healed.GetValue("Amount", 0) == 2 && ReferenceEquals(healed.GetObject("SourceNpc"), owner),
                "受到治疗时的主体为目标，载荷记录实际回复量和施术者");
        });
        EventMgr.Instance.RegisterEvent("tca-heal", "OnHeal", ScopeKey.Global, listener);
        try
        {
            heal.Call(ctx, "Self", -1);
            heal.Call(ctx, "Self", 5);
            heal.Call(ctx, "Self", 5);
            Check(owner.GetHp() == maxHp && healedHp.SequenceEqual([maxHp]), "治疗上限和实际治疗事件");
        }
        finally
        {
            EventMgr.Instance.RemoveEvent("OnHeal", ScopeKey.Global, listener);
        }

        using var addStat = lua.GetFunction("AddStatBuff");
        addStat.Call(ctx, "Self", "HpMax", 3, "Flat", "tca-test-hp-cap");
        Check(owner.GetCombatHpMax() == maxHp + 3 && owner.GetHp() == maxHp,
            "直接增加生命上限立即生效，但不等同于治疗");
        heal.Call(ctx, "Self", 5);
        Check(owner.GetHp() == maxHp + 3, "治疗可填满新增的生命上限");
        owner.ApplyDamage(new DamageInfo(opponent, null, owner, 3));
        owner.RemoveStatBuff("tca-test-hp-cap");

        using var reduce = lua.GetFunction("ReduceMP");
        using var recover = lua.GetFunction("RecoverMP");
        reduce.Call(ctx, "Self", owner.GetMp() + 10);
        Check(owner.GetMp() == 0, "直接减 MP 不低于零");
        recover.Call(ctx, "Self", owner.GetCombatMpMax() + 10);
        Check(owner.GetMp() == owner.GetCombatMpMax(), "直接回 MP 不超过上限");
        reduce.Call(ctx, "Self", owner.GetMp());
        owner.AddModifier("Element_Buff_Shui", 2);
        opponent.DoManaDraw();
        Check(owner.GetMp() == 0, "浩瀚不响应对手的基础补灵");
        var mana = owner.Backpack.Mana.Count;
        owner.DoManaDraw();
        Check(owner.GetMp() == 0 && owner.Backpack.Mana.Count == mana + 2, "浩瀚的 2 MP 能用于当次基础补灵");
    }

    private static void VerifyHealingBuffs(Lua lua, CombatNpc owner, CombatNpc opponent)
    {
        const string healOut = "All_HealOut_Changed";
        const string healOutOnce = "All_HealOut_Changed_Once";
        const string healIn = "All_HealIn_Changed";
        const string healInOnce = "All_HealIn_Changed_Once";
        var ctx = new APIContext { Caster = owner, Scene = owner.Scene };
        using var heal = lua.GetFunction("Heal");
        using var add = lua.GetFunction("AddNpcBuff");
        int maxHp = opponent.GetCombatHpMax();
        var healedHp = new List<int>();
        var listener = new DelegateEventListener((_, _, args) =>
        {
            var healed = ((APIContext)args).Caster;
            Check(healed == opponent, "治疗事件仍以实际回复的角色为主体");
            healedHp.Add(healed.GetHp());
        });
        EventMgr.Instance.RegisterEvent("tca-healing-buffs", "OnHeal", ScopeKey.Global, listener);
        try
        {
            add.Call(ctx, "Self", healOut, 2, -1);
            add.Call(ctx, "Target", healIn, -1, -1);
            add.Call(ctx, "Self", healOutOnce, 2, -1);
            add.Call(ctx, "Target", healInOnce, -8, -1);

            heal.Call(ctx, "Target", 5);
            opponent.ApplyDamage(new DamageInfo(owner, null, opponent, 6));
            heal.Call(ctx, "Target", 0);
            Check(opponent.GetHp() == maxHp - 6 && healedHp.Count == 0
                && owner.GetAllModifiers().Any(mod => mod.DefineId == healOutOnce)
                && opponent.GetAllModifiers().Any(mod => mod.DefineId == healInOnce),
                "满血和零治疗保留双方的单次治疗状态，且不派发治疗事件");

            heal.Call(ctx, "Target", 3);
            Check(opponent.GetHp() == maxHp - 6 && healedHp.Count == 0
                && owner.GetAllModifiers().All(mod => mod.DefineId != healOutOnce)
                && opponent.GetAllModifiers().All(mod => mod.DefineId != healInOnce),
                "受疗降低可把治疗扣至零，两侧单次状态均已兑现");

            heal.Call(ctx, "Target", 3);
            heal.Call(ctx, "Target", 30);
            Check(opponent.GetHp() == maxHp && healedHp.SequenceEqual([maxHp - 2, maxHp]),
                "持续治疗提高与受疗降低分别作用在施术者和目标，超出部分按血量上限截断");

            opponent.ApplyDamage(new DamageInfo(owner, null, opponent, 2));
            opponent.ApplyHeal(2);
            Check(opponent.GetHp() == maxHp - 1 && healedHp[^1] == maxHp - 1,
                "无指定来源的治疗仍受目标的受疗降低影响");
        }
        finally
        {
            EventMgr.Instance.RemoveEvent("OnHeal", ScopeKey.Global, listener);
            owner.RemoveModifier(healOut);
            owner.RemoveModifier(healOutOnce);
            opponent.RemoveModifier(healIn);
            opponent.RemoveModifier(healInOnce);
        }
        opponent.ApplyHeal(1);
    }

    private static void VerifyResourceValueBuffs(Lua lua)
    {
        var a = BackpackModelTests.CreateNpc();
        var b = BackpackModelTests.CreateNpc();
        a.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-test"));
        b.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-test"));
        var scene = new CombatScene();
        scene.Init(7365, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 200);
        try
        {
            scene.PreStart();
            scene.Start();
            var owner = scene.GetCombatants()[a.Id];
            var opponent = scene.GetCombatants()[b.Id];
            var ctx = new APIContext { Caster = owner, Scene = scene };
            VerifyScriptedModifierHooks(lua, owner);
            VerifyShieldGainBuffs(lua, ctx, owner, opponent);
            VerifyMpRecoveryBuffs(lua, ctx, owner, opponent);
        }
        finally
        {
            scene.Cleanup();
        }
    }

    // 两个普通状态直接定义获盾 Hook，验证 Lua 改值、消费与移除回调重入。
    private static void VerifyScriptedModifierHooks(Lua lua, CombatNpc owner)
    {
        void Attach(string id, string script)
        {
            owner.AddModifier(id);
            var buff = owner.GetAllModifiers().Single(mod => mod.DefineId == id);
            buff.env = (LuaTable)lua.DoString(script)[0];
            buff.env["owner"] = owner;
            buff.env["m_Self"] = buff;
            buff.ScanLuaHooks();
        }

        var before = owner.ShieldValue;
        try
        {
            Attach("Armor", """
                return {
                    ModifyShieldGain = function(self, ctx)
                        ctx:Set("Amount", ctx:GetObject("Amount") + 3)
                        self.m_Self:Consume()
                    end,
                    OnRemove = function(self, ctx) self.owner:ChangeShield(1) end
                }
                """);
            Attach("Fortification", """
                return {
                    ModifyShieldGain = function(self, ctx)
                        ctx:Set("Amount", ctx:GetObject("Amount") + 4)
                        self.m_Self:Consume()
                    end
                }
                """);
            owner.ChangeShield(2);
            Check(owner.ShieldValue == before + 10 && owner.GetAllModifiers().All(m => m.DefineId != "Fortification" && m.DefineId != "Armor"),
                "基础 2 与两份单次 3、4 合计 9，移除触发的新获盾只获得 1");
            Attach("Armor", "return { ModifyShieldGain = function(self, ctx) ctx:Set('Amount', 5) return 123 end }");
            owner.ChangeShield(1);
            Check(owner.ShieldValue == before + 15, "脚本可直接修改 Amount，旧回调的单个整数返回值不计入增量");
        }
        finally
        {
            owner.RemoveModifier("Armor");
            owner.RemoveModifier("Fortification");
            owner.ChangeShield(before - owner.ShieldValue);
        }
    }

    private static void VerifyShieldGainBuffs(Lua lua, APIContext ctx, CombatNpc owner, CombatNpc opponent)
    {
        const string gainId = "All_ShieldGain_Changed";
        const string onceId = "All_ShieldGain_Changed_Once";
        using var gain = lua.GetFunction("GainShield");
        using var armorBreak = lua.GetFunction("ArmorBreak");
        owner.AddModifier(gainId, 2);
        opponent.AddModifier(gainId, -2);
        gain.Call(ctx, "Self", 5);
        gain.Call(ctx, "Target", 5);
        Check(owner.ShieldValue == 7 && opponent.ShieldValue == 3, "双方按各自的灵盾获得量修正加盾");

        owner.AddModifier(onceId, 2);
        opponent.AddModifier(onceId, -5);
        owner.ApplyDamage(new DamageInfo(opponent, null, owner, 1));
        armorBreak.Call(ctx, "Target", 1);
        gain.Call(ctx, "Self", 0);
        Check(owner.ShieldValue == 6 && opponent.ShieldValue == 2
            && owner.GetAllModifiers().Any(mod => mod.DefineId == onceId)
            && opponent.GetAllModifiers().Any(mod => mod.DefineId == onceId),
            "承伤扣盾、破盾和无效加盾均保留单次获得量修正");

        gain.Call(ctx, "Self", 1);
        gain.Call(ctx, "Target", 5);
        Check(owner.ShieldValue == 11 && opponent.ShieldValue == 2
            && owner.GetAllModifiers().All(mod => mod.DefineId != onceId)
            && opponent.GetAllModifiers().All(mod => mod.DefineId != onceId),
            "持续和单次修正可以一起生效，扣至零的加盾也消费单次修正");
        gain.Call(ctx, "Target", 5);
        Check(opponent.ShieldValue == 5, "后续只保留持续灵盾获得量降低");
    }

    private static void VerifyMpRecoveryBuffs(Lua lua, APIContext ctx, CombatNpc owner, CombatNpc opponent)
    {
        const string mpId = "All_MpRecovery_Changed";
        const string onceId = "All_MpRecovery_Changed_Once";
        using var recover = lua.GetFunction("RecoverMP");
        using var convert = lua.GetFunction("Convert");
        int ownerMax = owner.GetCombatMpMax();
        int opponentMax = opponent.GetCombatMpMax();
        owner.AddModifier(mpId, 2);
        owner.AddModifier(onceId, 3);
        recover.Call(ctx, "Self", 3);
        Check(owner.GetMp() == ownerMax && owner.GetAllModifiers().Any(mod => mod.DefineId == onceId),
            "满 MP 时保留单次回复提高");

        owner.ReduceMP(8);
        recover.Call(ctx, "Self", 0);
        owner.Backpack.PlaceMana(owner.Backpack.EmptyCells()[0], ElementType.Jin);
        convert.Call(ctx, "Self", "Jin", 1);
        Check(owner.GetMp() == ownerMax - 7 && owner.Backpack.Mana.Count == 0
            && owner.GetAllModifiers().Any(mod => mod.DefineId == onceId),
            "耗蓝、零回复和灵元回收保留单次回复提高，回收一格只增加 1 MP");
        recover.Call(ctx, "Self", 3);
        Check(owner.GetMp() == ownerMax && owner.GetAllModifiers().All(mod => mod.DefineId != onceId),
            "直接回复按持续和单次修正结算，上限不足时按缺失 MP 截断");

        owner.ReduceMP(8);
        recover.Call(ctx, "Self", 3);
        Check(owner.GetMp() == ownerMax - 3, "后续直接回复只应用持续提高");
        owner.AddModifier("Element_Buff_Shui", 1);
        owner.DoManaDraw();
        Check(owner.GetMp() == ownerMax, "浩瀚的直接回复量也受修正");

        opponent.ReduceMP(10);
        opponent.AddModifier(mpId, -2);
        opponent.AddModifier(onceId, -5);
        recover.Call(ctx, "Target", 4);
        Check(opponent.GetMp() == opponentMax - 10 && opponent.GetAllModifiers().All(mod => mod.DefineId != onceId),
            "MP 回复被降低至零时消费单次修正");
        recover.Call(ctx, "Target", 5);
        Check(opponent.GetMp() == opponentMax - 7, "后续只应用持续 MP 回复量降低");
    }

    private static void VerifyContestEvents(CombatNpc owner, CombatNpc opponent)
    {
        var attacker = owner.GetField().First(card => card.DefineId == "bp_attack");
        var rival = opponent.GetField().First(card => card.DefineId == "bp_attack");
        var losing = new ContestData { SourceCard = attacker, ContestValue = 4, ContestType = ContestType.Zhan };
        var winning = new ContestData { SourceCard = rival, ContestValue = 5, ContestType = ContestType.Zhan };
        var attacks = new List<CombatCard>();
        var results = new List<APIContext>();
        var attackListener = new DelegateEventListener((_, _, args) => attacks.Add(((APIContext)args).SourceCard));
        var contestListener = new DelegateEventListener((_, _, args) => results.Add((APIContext)args));
        EventMgr.Instance.RegisterEvent("tca-attack", "OnAttack", ScopeKey.Global, attackListener);
        EventMgr.Instance.RegisterEvent("tca-contest", "OnContestOverflow", ScopeKey.Global, contestListener);
        try
        {
            owner.ResolveContest(losing, winning);
            owner.Straight(losing);
            Check(attacks.SequenceEqual([rival, attacker, attacker]), "胜方、负方与直击均派发攻击事件");
            var result = results.Single();
            Check(result.Get<CombatNpc>("Winner") == opponent && result.Get<CombatNpc>("Loser") == owner
                && result.Get<CombatCard>("WinnerCard") == rival && result.Get<CombatCard>("LoserCard") == attacker
                && result.GetValue("Overflow", 0) == 1, "胜差只来自实际对拼且保留结果字段");
        }
        finally
        {
            EventMgr.Instance.RemoveEvent("OnAttack", ScopeKey.Global, attackListener);
            EventMgr.Instance.RemoveEvent("OnContestOverflow", ScopeKey.Global, contestListener);
        }
    }

    private static void VerifyContestPair(Lua lua, CombatNpc owner, CombatNpc opponent)
    {
        var attacker = owner.GetField().First(card => card.DefineId == "bp_attack");
        var rival = opponent.GetField().First(card => card.DefineId == "bp_attack");
        var first = new ContestData { SourceCard = attacker, ContestValue = 4, ContestType = ContestType.Zhan };
        var other = new ContestData { SourceCard = rival, ContestValue = 5, ContestType = ContestType.Ci };
        owner.AddContestData(first);
        opponent.AddContestData(other);

        var pairs = new List<APIContext>();
        var results = new List<APIContext>();
        var pairedListener = new DelegateEventListener((_, _, args) => pairs.Add((APIContext)args));
        var resultListener = new DelegateEventListener((_, _, args) => results.Add((APIContext)args));
        EventMgr.Instance.RegisterEvent("tca-paired", "OnContest", ScopeKey.Global, pairedListener);
        EventMgr.Instance.RegisterEvent("tca-paired-result", "OnContestOverflow", ScopeKey.Global, resultListener);
        try
        {
            owner.ProcessContest();
            var paired = pairs.Single();
            Check(paired.Get<CombatNpc>("ContestA") == owner && paired.Get<CombatNpc>("ContestB") == opponent
                && paired.Get<CombatCard>("ContestCardA") == attacker && paired.Get<CombatCard>("ContestCardB") == rival
                && paired.GetStringValue("ContestTypeA") == "Zhan" && paired.GetStringValue("ContestTypeB") == "Ci",
                "配对事件提供双方角色、来源及拼点类型");
            var result = results.Single();
            Check(result.Get<CombatNpc>("Winner") == opponent && result.GetValue("WinnerValue", 0) == 5
                && result.GetObject("IsDraw") is false && result.GetValue("Overflow", 0) == 1,
                "按入槽的 4 对 5 判胜负");
            Check(result.Caster == opponent && result.SourceCard == rival
                && result.GetValue("ContestValueA", 0) == 4 && result.GetValue("ContestValueB", 0) == 5,
                "胜负事件保留胜方来源和双方最终拼点");
            using var outcome = lua.GetFunction("CheckContestOutcome");
            Check((bool)outcome.Call(result, opponent, "Win")[0] && (bool)outcome.Call(result, owner, "Lose")[0]
                && !(bool)outcome.Call(result, owner, "Win")[0] && !(bool)outcome.Call(result, owner, "Draw")[0],
                "对拼结果条件按实际参战方判断");

            owner.ResolveContest(new ContestData { SourceCard = attacker, ContestValue = 5 }, other);
            Check(pairs.Count == 2 && results.Count == 2, "平局和胜负均报告一次配对与结算");
            var draw = results[1];
            Check(draw.GetObject("IsDraw") is true && draw.GetValue("Overflow", -1) == 0
                && draw.Get<CombatNpc>("Winner") == null && draw.Get<CombatNpc>("Loser") == null,
                "平局差值为零，没有胜负角色");
            Check((bool)outcome.Call(draw, owner, "Draw")[0] && (bool)outcome.Call(draw, opponent, "Draw")[0]
                && !(bool)outcome.Call(draw, owner, "Win")[0] && !(bool)outcome.Call(draw, owner, "Lose")[0],
                "平局条件匹配双方，胜负条件均不匹配");
            owner.Straight(first);
            Check(pairs.Count == 2 && results.Count == 2, "轮末直击不派发配对或对拼结算");
        }
        finally
        {
            EventMgr.Instance.RemoveEvent("OnContest", ScopeKey.Global, pairedListener);
            EventMgr.Instance.RemoveEvent("OnContestOverflow", ScopeKey.Global, resultListener);
        }
    }

    private static void VerifyDamageDefense(Lua lua, CombatNpc owner, CombatNpc opponent)
    {
        var ctx = new APIContext { Caster = owner, Scene = owner.Scene };
        using var shield = lua.GetFunction("GainShield");
        using var armor = lua.GetFunction("AddArmor");
        using var fortify = lua.GetFunction("AddFortification");
        using var damage = lua.GetFunction("SelfDamage");
        var hp = owner.GetHp();
        shield.Call(ctx, "Self", 4);
        armor.Call(ctx, "Self", 2);
        fortify.Call(ctx, "Self", 1);

        owner.ApplyDamage(new DamageInfo(owner, null, owner, 1));
        Check(owner.GetHp() == hp && owner.ShieldValue == 4, "自伤受坚甲影响但不消耗一次性护甲");
        owner.ApplyDamage(new DamageInfo(opponent, null, owner, 5));
        Check(owner.GetHp() == hp && owner.ShieldValue == 2, "5 点先扣 2 护甲和 1 坚甲再扣 2 灵盾");
        owner.ApplyDamage(new DamageInfo(opponent, null, owner, 5));
        Check(owner.GetHp() == hp - 2 && owner.ShieldValue == 0, "第二击只扣坚甲，再扣余盾和 2 HP");
        for (int row = 0; row < owner.Backpack.Height; row++) owner.Backpack.FinishRow();
        owner.ApplyDamage(new DamageInfo(opponent, null, owner, 1));
        Check(owner.GetHp() == hp - 3, "持有者进入新一轮，坚甲已失效");

        opponent.DealDamage();
        var enemyHp = opponent.GetHp();
        armor.Call(ctx, "Target", 2);
        damage.Call(ctx, "Target", 3);
        opponent.DealDamage();
        Check(opponent.GetHp() == enemyHp - 1 &&
            opponent.GetAllModifiers().All(modifier => modifier.DefineId != "Armor"),
            "对敌方排入的直接伤害会消耗护甲");

        armor.Call(ctx, "Target", 2);
        opponent.AddModifier("Element_Debuff_Jin", 1);
        opponent.DoManaDraw();
        opponent.DealDamage();
        Check(opponent.GetHp() == enemyHp - 2 &&
            opponent.GetAllModifiers().Any(modifier => modifier.DefineId == "Armor" && modifier.CurrentStack == 2),
            "出血造成自伤，保留一次性护甲");
        opponent.RemoveModifier("Armor");
        opponent.RemoveModifier("Element_Debuff_Jin");
    }

    private static void VerifyDamageValueBuffs(Lua lua)
    {
        var a = BackpackModelTests.CreateNpc();
        var b = BackpackModelTests.CreateNpc();
        var scene = new CombatScene();
        scene.Init(7361, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 200);
        try
        {
            scene.PreStart();
            scene.Start();
            var source = scene.GetCombatants()[a.Id];
            var target = scene.GetCombatants()[b.Id];
            var ctx = new APIContext { Caster = target, Scene = scene };
            using var shield = lua.GetFunction("GainShield");
            using var armor = lua.GetFunction("AddArmor");
            bool Has(string id) => target.GetAllModifiers().Any(mod => mod.DefineId == id);
            int hp = target.GetHp();

            source.AddModifier("Zhan_DamageOut_Changed", 2);
            source.AddModifier("Huo_DamageOut_Changed", -1);
            target.AddModifier("Huo_DamageIn_Changed", -2);
            armor.Call(ctx, "Self", 1);
            shield.Call(ctx, "Self", 3);
            var strike = new DamageInfo(source, null, target, 8, DamageType.Zhan, ElementType.Huo);
            target.AddDamage(strike);
            Check(strike.Damage == 9, "火系斩击同时匹配物理和五行出伤");
            target.DealDamage();
            Check(strike.Damage == 7 && target.GetHp() == hp - 3 && target.ShieldValue == 0 && !Has("Armor"),
                "入伤修正在护甲和灵盾处理前结算");
            source.RemoveModifier("Zhan_DamageOut_Changed");
            source.RemoveModifier("Huo_DamageOut_Changed");
            target.RemoveModifier("Huo_DamageIn_Changed");

            const string nextFireHit = "Huo_DamageIn_Changed_Once";
            target.AddModifier(nextFireHit, -2);
            shield.Call(ctx, "Self", 4);
            target.AddDamage(new DamageInfo(source, null, target, 1, DamageType.Ci, ElementType.Mu));
            target.DealDamage();
            Check(Has(nextFireHit) && target.ShieldValue == 3, "非火伤害保留火抗性");
            target.AddDamage(new DamageInfo(source, null, target, 3, DamageType.Ci, ElementType.Huo));
            target.DealDamage();
            Check(!Has(nextFireHit) && target.ShieldValue == 2 && target.GetHp() == hp - 3,
                "匹配伤害被灵盾全吸收仍消费对应的下次抗性");

            const string nextFireOut = "Huo_DamageOut_Changed_Once";
            source.AddModifier(nextFireOut, -4);
            target.AddModifier(nextFireHit, 3);
            target.AddDamage(new DamageInfo(source, null, target, 0, DamageType.Ci, ElementType.Huo));
            target.DealDamage();
            Check(Has(nextFireHit) && source.GetAllModifiers().Any(mod => mod.DefineId == nextFireOut),
                "零伤害保留出伤与入伤的下次效果");
            target.AddDamage(new DamageInfo(source, null, target, 2, DamageType.Ci, ElementType.Huo));
            target.DealDamage();
            Check(Has(nextFireHit) && target.ShieldValue == 2
                && source.GetAllModifiers().All(mod => mod.DefineId != nextFireOut),
                "首次匹配的出伤降至零时消费出伤 Buff，保留入伤 Buff");
            target.RemoveModifier(nextFireHit);

            target.AddModifier("Zhan_DamageIn_Changed", -1);
            var unsourced = new DamageInfo { Damage = 3, TargetNpc = target };
            target.ApplyDamage(unsourced);
            Check(unsourced.Damage == 2, "无来源斩伤也受斩击入伤修正");
            target.AddModifier("Zhan_DamageIn_Changed", -2);
            Check(target.GetAllModifiers().Single(mod => mod.DefineId == "Zhan_DamageIn_Changed").CurrentStack == -3,
                "斩击入伤修正的 -1 层与 -2 层合并为 -3 层");
            target.RemoveModifier("Zhan_DamageIn_Changed");
            Check(!Has("Zhan_DamageIn_Changed"), "持有者可移除斩击入伤修正");
            source.AddModifier("Ci_DamageOut_Changed", 2);
            var stab = new DamageInfo(source, null, target, 1, DamageType.Ci, ElementType.None);
            target.AddDamage(stab);
            Check(stab.Damage == 3, "刺击出伤修正每层增加一点伤害");
            target.DealDamage();
            source.RemoveModifier("Ci_DamageOut_Changed");

            // 从真实入队入口观察同一个 DamageInfo。大数用整数初始化，避免 float 构造参数舍入。
            long Outgoing(int damage)
            {
                var hit = new DamageInfo { SourceNpc = source, TargetNpc = target, Damage = damage, damageType = DamageType.Ci };
                target.AddDamage(hit);
                return hit.Damage;
            }

            const string all = "All_DamageOut_Changed";
            const string stabOnce = "Ci_DamageOut_Changed_Once";
            source.AddModifier(all, 2);
            Check(Outgoing(10) == 12 && Outgoing(10) == 12, "全出伤 Lua 直接改值，持续状态可以重复生效");
            source.RemoveModifier(all);
            foreach (bool weakenFirst in new[] { true, false })
            {
                if (weakenFirst) source.AddModifier(all, -5);
                source.AddModifier(stabOnce, 4);
                if (!weakenFirst) source.AddModifier(all, -5);
                Check(Outgoing(2) == 1 && source.GetAllModifiers().All(m => m.DefineId != stabOnce),
                    "两项出伤修正按任意施加顺序均为 2 - 5 + 4，单次只兑现一次");
                source.RemoveModifier(all);
            }
            source.AddModifier(all, 99);
            source.AddModifier(stabOnce, -99);
            Check(Outgoing(int.MaxValue - 1) == int.MaxValue - 1, "同阶段中间值越过 int 上限后仍能完整抵消");
            Check(Outgoing(int.MaxValue) == int.MaxValue, "出伤提交时钳制到 int 上限");
            source.RemoveModifier(all);
        }
        finally
        {
            scene.Cleanup();
        }
    }

    private static void VerifyDeflection(Lua lua, CombatNpc owner, CombatNpc opponent)
    {
        var attacker = owner.GetField().First(card => card.DefineId == "bp_attack");
        var rival = opponent.GetField().First(card => card.DefineId == "bp_attack");
        var ctx = new APIContext { Caster = owner, SourceCard = attacker, Scene = owner.Scene };
        using var add = lua.GetFunction("AddDeflection");
        add.Call(ctx, "Self", 1);
        add.Call(ctx, "Self", 2);
        int DeflectionStacks() => owner.GetAllModifiers().Where(modifier => modifier.DefineId == "Deflection")
            .Sum(modifier => modifier.CurrentStack);
        Check(DeflectionStacks() == 3, "卸力按常规 Buff 叠层");

        var straight = new ContestData { SourceCard = rival, ContestValue = 4, ContestType = ContestType.Zhan };
        opponent.Straight(straight);
        Check(straight.ContestValue == 4 && DeflectionStacks() == 3, "敌方直击沿用原点数并保留卸力");

        var strike = new ContestData { SourceCard = attacker, ContestValue = 4, ContestType = ContestType.Zhan };
        var defense = new ContestData { SourceCard = rival, ContestValue = 5, ContestType = ContestType.Block };
        owner.ResolveContest(strike, defense);
        Check(strike.ContestValue == 4 && defense.ContestValue == 5 && DeflectionStacks() == 3, "敌方防御配对保留卸力");

        var enemyStrike = new ContestData { SourceCard = rival, ContestValue = 5, ContestType = ContestType.Zhan };
        owner.ResolveContest(strike, enemyStrike);
        Check(strike.ContestValue == 4 && enemyStrike.ContestValue == 2, "攻击对攻击时卸力只扣敌方点数");
        Check(owner.GetAllModifiers().All(modifier => modifier.DefineId != "Deflection"), "卸力消费同步移除可见状态");
        enemyStrike.ContestValue = 5;
        owner.ResolveContest(strike, enemyStrike);
        Check(strike.ContestValue == 4 && enemyStrike.ContestValue == 5, "卸力只在首次符合条件时消费");

        var rivalCtx = new APIContext { Caster = opponent, SourceCard = rival, Scene = opponent.Scene };
        add.Call(rivalCtx, "Target", 3);
        Check(DeflectionStacks() == 3, "卸力独立动作支持按目标施加");
        var guard = new ContestData { SourceCard = attacker, ContestValue = 4, ContestType = ContestType.Block };
        enemyStrike.ContestValue = 2;
        opponent.ResolveContest(enemyStrike, guard);
        Check(guard.ContestValue == 4 && enemyStrike.ContestValue == 0 && DeflectionStacks() == 0,
            "持有者用防御配对时也消耗卸力，敌方点数最低为零");
    }

    private static void VerifyContestBuffs(Lua lua, CombatNpc owner)
    {
        using var add = lua.GetFunction("AddNpcBuff");
        var ctx = new APIContext { Caster = owner, Scene = owner.Scene };
        var cases = new (string Id, ContestType Match, ElementType Element, ContestType Other, ElementType OtherElement)[]
        {
            ("Ci", ContestType.Ci, ElementType.Jin, ContestType.Zhan, ElementType.Jin),
            ("Zhan", ContestType.Zhan, ElementType.Jin, ContestType.Ci, ElementType.Jin),
            ("Da", ContestType.Da, ElementType.Jin, ContestType.SheJi, ElementType.Jin),
            ("SheJi", ContestType.SheJi, ElementType.Jin, ContestType.Da, ElementType.Jin),
            ("Shield", ContestType.Shield, ElementType.None, ContestType.Block, ElementType.None),
            ("Block", ContestType.Block, ElementType.None, ContestType.Dodge, ElementType.None),
            ("Dodge", ContestType.Dodge, ElementType.None, ContestType.Shield, ElementType.None),
            ("Jin", ContestType.Ci, ElementType.Jin, ContestType.Ci, ElementType.Huo),
            ("Mu", ContestType.Ci, ElementType.Mu, ContestType.Ci, ElementType.Jin),
            ("Shui", ContestType.Ci, ElementType.Shui, ContestType.Ci, ElementType.Mu),
            ("Huo", ContestType.Ci, ElementType.Huo, ContestType.Ci, ElementType.Tu),
            ("Tu", ContestType.Ci, ElementType.Tu, ContestType.Ci, ElementType.Shui),
            ("Attack", ContestType.SheJi, ElementType.Jin, ContestType.Dodge, ElementType.None),
            ("Defend", ContestType.Dodge, ElementType.None, ContestType.SheJi, ElementType.Jin)
        };

        int Modify(ContestType type, ElementType element)
        {
            var contest = new ContestData { ContestType = type, Element = element, ContestValue = 4 };
            ctx.Set("ContestData", contest);
            owner.ModifyHook("Contest", ctx);
            return contest.FinalValue;
        }

        foreach (var (id, match, element, other, otherElement) in cases)
        {
            var always = id + "_Changed";
            var once = always + "_Once";
            add.Call(ctx, "Self", always, 1, -1);
            add.Call(ctx, "Self", once, 2, -1);
            Check(Modify(other, otherElement) == 4 && owner.GetAllModifiers().Any(mod => mod.DefineId == once),
                id + " 的强化跳过不匹配拼点");
            Check(Modify(match, element) == 7 && owner.GetAllModifiers().All(mod => mod.DefineId != once),
                id + " 的下次强化只在匹配时生效并移除");
            Check(Modify(match, element) == 5, id + " 的持续强化在后续匹配拼点继续生效");
            owner.RemoveModifier(always);

            add.Call(ctx, "Self", once, -6, -1);
            Check(Modify(other, otherElement) == 4 && owner.GetAllModifiers().Any(mod => mod.DefineId == once),
                id + " 的弱化跳过不匹配拼点");
            Check(Modify(match, element) == 0 && Modify(match, element) == 4,
                id + " 的下次弱化限制为零并仅消耗一次");
        }

        add.Call(ctx, "Self", "Ci_Changed_Once", -6, -1);
        add.Call(ctx, "Self", "Attack_Changed_Once", 3, -1);
        add.Call(ctx, "Self", "Jin_Changed", 2, -1);
        Check(Modify(ContestType.Ci, ElementType.Jin) == 3
            && Modify(ContestType.Ci, ElementType.Jin) == 6,
            "金系刺击汇总全攻击、刺击、金系改变后限幅，仅移除两项下次状态");
        Check(Modify(ContestType.Block, ElementType.Jin) == 4, "五行拼点变化仅匹配攻击");
        owner.RemoveModifier("Jin_Changed");
    }

    private static void VerifyValueBuffStacks(CombatNpc owner)
    {
        const string id = "Ci_Changed";
        int? Layers() => owner.GetAllModifiers().SingleOrDefault(mod => mod.DefineId == id)?.CurrentStack;
        owner.AddModifier(id, 0);
        Check(Layers() == null, "零层不创建数值状态");
        owner.AddModifier(id, -3);
        Check(Layers() == -3, "首次负层进入战斗状态列表");
        owner.AddModifier(id, 1);
        Check(Layers() == -2, "正负层按代数和叠加");
        owner.AddModifier(id, 2);
        Check(Layers() == null, "互抵归零后立即移除");

        owner.AddModifier(id, int.MinValue);
        Check(Layers() == -99, "首次负层的绝对值受限");
        owner.AddModifier(id, int.MinValue);
        Check(Layers() == -99, "极小整数叠加不会回绕");
        owner.AddModifier(id, int.MaxValue);
        Check(Layers() == 99, "正层以相同上限钳制");
        owner.AddModifier(id, int.MaxValue);
        Check(Layers() == 99, "极大整数叠加不会回绕");
        owner.RemoveModifier(id);

        owner.AddModifier(id, -2, 3);
        owner.ModifierTick();
        owner.AddModifier(id, -1, 10);
        owner.ModifierTick();
        Check(Layers() == -3, "有限期的负层在最后一 Tick 前仍生效");
        owner.ModifierTick();
        Check(Layers() == null, "叠层沿用首次施加的到期时间");
    }
}
