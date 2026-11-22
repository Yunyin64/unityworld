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
        var listener = new DelegateEventListener((_, _, args) => healedHp.Add(((APIContext)args).Caster.GetHp()));
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
        var buffIds = ActionDefineMgr.Instance.Get("action_add_npc_buff").ParamDefs.Single(param => param.Name == "BuffId")
            .Value.Select(value => value.GetString()).ToHashSet();
        var cases = new (string Id, ContestType Match, ElementType Element, ContestType Other, ElementType OtherElement)[]
        {
            ("Ci", ContestType.Ci, ElementType.Jin, ContestType.Zhan, ElementType.Jin),
            ("Zhan", ContestType.Zhan, ElementType.Jin, ContestType.Ci, ElementType.Jin),
            ("Da", ContestType.Da, ElementType.Jin, ContestType.SheJi, ElementType.Jin),
            ("SheJi", ContestType.SheJi, ElementType.Jin, ContestType.Da, ElementType.Jin),
            ("Shield", ContestType.Shield, ElementType.None, ContestType.Block, ElementType.None),
            ("Block", ContestType.Block, ElementType.None, ContestType.Dodge, ElementType.None),
            ("Doge", ContestType.Dodge, ElementType.None, ContestType.Shield, ElementType.None),
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
            return contest.ContestValue;
        }

        foreach (var (id, match, element, other, otherElement) in cases)
        {
            var always = id + "_Improved";
            var once = always + "_Once";
            Check(buffIds.Contains(always) && buffIds.Contains(once), id + " 的两种 Buff 可通过 TCA 选取");
            add.Call(ctx, "Self", always, 1, -1);
            add.Call(ctx, "Self", once, 2, -1);
            Check(Modify(other, otherElement) == 4 && owner.GetAllModifiers().Any(mod => mod.DefineId == once),
                id + " 的强化跳过不匹配拼点");
            Check(Modify(match, element) == 7 && owner.GetAllModifiers().All(mod => mod.DefineId != once),
                id + " 的下次强化只在匹配时生效并移除");
            Check(Modify(match, element) == 5, id + " 的持续强化在后续匹配拼点继续生效");
            owner.RemoveModifier(always);
        }
    }
}
