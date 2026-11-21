using System.Text.Json;
using System.Text.RegularExpressions;
using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests;

public static class TcaAlignmentTests
{
    private static int _assertions;

    public static void Run()
    {
        _assertions = 0;
        var dataDir = Path.GetDirectoryName(CardDefineMgr.Instance.DataDir)!;
        using var lua = new Lua();
        lua["API"] = APIMgr.Instance;
        foreach (var file in new[] { "CombatCard_Condition", "CombatCard_Action", "CombatNpc_Action", "CombatNpc_Contest" })
            lua.DoString(File.ReadAllText(Path.Combine(dataDir, "LuaScripts", file + ".lua")));

        VerifyCatalog(lua, dataDir);
        var a = BackpackModelTests.CreateNpc();
        var b = BackpackModelTests.CreateNpc();
        a.Stats.AddModifier("ManaDrawCost", StatModifier.Override(2, "tca-test"));
        b.Stats.AddModifier("ManaDrawCost", StatModifier.Override(0, "tca-test"));
        BackpackModelTests.Place(a, "bp_attack", 1, 0);
        BackpackModelTests.Place(a, "bp_focus", 2, 0);
        BackpackModelTests.Place(b, "bp_attack", 1, 0);
        var scene = new CombatScene();
        scene.Init(7351, [(a, CombatTeam.TeamA), (b, CombatTeam.TeamB)], 200);
        try
        {
            scene.PreStart();
            scene.Start();
            var owner = scene.GetCombatants()[a.Id];
            var opponent = scene.GetCombatants()[b.Id];
            VerifySelectors(lua, owner, opponent);
            VerifyResourceSettlement(lua, owner, opponent);
            VerifyContestEvents(owner, opponent);
            VerifyWoundSelection(lua, owner);
        }
        finally
        {
            scene.Cleanup();
        }
        Console.WriteLine($"TCA_ALIGNMENT_TESTS_OK assertions={_assertions}");
    }

    private static void VerifyCatalog(Lua lua, string dataDir)
    {
        VerifyJsonCatalog([Path.Combine(dataDir, "TriggerDefines.json")],
            TriggerDefineMgr.Instance.GetAll().Select(entry => entry.ID));

        var actions = ActionDefineMgr.Instance.GetAll().ToArray();
        var actionIds = VerifyJsonCatalog(Directory.EnumerateFiles(Path.Combine(dataDir, "Action"), "*.json"),
            actions.Select(entry => entry.ID));
        Check(actions.Any(entry => entry.FuncName == "RecoverMP")
            && actions.Any(entry => entry.FuncName == "ReduceMP"), "目录收录直接增减 MP");
        foreach (var entry in actions.Where(entry => actionIds.Contains(entry.ID)))
            VerifyTemplate(lua, entry.ID, entry.LuaTemplate, entry.ParamDefs, entry.FuncName);

        var conditions = ConditionDefineMgr.Instance.GetAll().ToArray();
        var conditionIds = VerifyJsonCatalog(Directory.EnumerateFiles(Path.Combine(dataDir, "Condition"), "*.json"),
            conditions.Select(entry => entry.ID));
        foreach (var entry in conditions.Where(entry => entry.ID != "cond_none" && conditionIds.Contains(entry.ID)))
            VerifyTemplate(lua, entry.ID, entry.LuaTemplate, entry.ParamDefs, "SelectCards");
    }

    private static HashSet<string> VerifyJsonCatalog(IEnumerable<string> files, IEnumerable<string> loadedIds)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var callableIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            using var json = JsonDocument.Parse(File.ReadAllText(file), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            foreach (var entry in json.RootElement.EnumerateArray())
            {
                var id = entry.GetProperty("ID").GetString()!;
                Check(ids.Add(id), "目录 ID 唯一：" + id);
                // 这里核对 JSON 宣称可用的模板。战斗执行入口独立于目录状态。
                var status = entry.TryGetProperty("Status", out var value) ? value.GetString() : "Ready";
                Check(status is "Ready" or "Pending", id + " 状态标注有效");
                if (status == "Pending")
                {
                    Check(entry.TryGetProperty("StatusReason", out var reason) && !string.IsNullOrWhiteSpace(reason.GetString()),
                        id + " 记录待接入原因");
                    continue;
                }
                callableIds.Add(id);
            }
        }
        Check(ids.SetEquals(loadedIds), "已加载目录保留全部配置 ID");
        return callableIds;
    }

    private static void VerifyTemplate(Lua lua, string id, string template, List<APIParamDef> parameters, string function)
    {
        // 展开发布的模板并读取真实包装设置的上下文，检出参数错位、缺失以及 Lua 类型错误。
        Check(template.StartsWith(function + "(ctx,", StringComparison.Ordinal), id + " 调用声明的 API");
        var api = APIMgr.Instance.Get(function);
        Check(api != null, id + " 已注册 API");
        var input = api!.ParamsList.Where(param => param.Name != "Result").ToArray();
        var args = template[(template.IndexOf(',') + 1)..^1].Split(',').Select(arg => arg.Trim()).ToArray();
        Check(args.Length == input.Length, id + " 参数个数匹配");
        for (int i = 0; i < args.Length; i++)
        {
            var placeholder = Regex.Match(args[i], @"^""?\{(\w+)\}""?$");
            if (!placeholder.Success) continue; // 固定元素、布尔字面量及空字符串来源 ID。
            var parameter = parameters.SingleOrDefault(p => p.Name == placeholder.Groups[1].Value);
            Check(parameter != null && parameter.Name == input[i].Name && parameter.Type == input[i].Type, id + " 参数 " + input[i].Name);
            Check(parameter!.Value.All(value => parameter.Type switch
            {
                "String" => value.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(value.GetString()),
                "Bool" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
                _ => value.ValueKind == JsonValueKind.Number
            }), id + " 候选类型 " + parameter.Name);
            var value = parameter.Value.First();
            template = template.Replace(placeholder.Value.Trim('"'), value.ValueKind == JsonValueKind.String
                ? value.GetString()! : value.GetRawText(), StringComparison.Ordinal);
        }
        var ctx = new APIContext();
        lua["sampleContext"] = ctx;
        lua.DoString("local ctx = sampleContext\n" + template);
        foreach (var param in input)
            Check(ctx.GetObject(param.Name) != null, id + " Lua 包装传递 " + param.Name);
    }

    private static void VerifySelectors(Lua lua, CombatNpc owner, CombatNpc opponent)
    {
        var ctx = new APIContext { Scene = owner.Scene, Caster = owner, SourceCard = owner.GetField()[0] };
        using var select = lua.GetFunction("SelectCards");
        var all = (List<CombatCard>)select.Call(ctx, "All")[0];
        Check(all.SequenceEqual(owner.GetField()), "选中全部己方场上卡");
        all.Clear();
        Check(owner.GetField().Count > 1, "修改结果不会修改场上列表");
        var adjacent = (List<CombatCard>)select.Call(ctx, "Adjacent")[0];
        Check(adjacent.SequenceEqual(owner.GetField().Skip(1).Take(1)), "场上顺序首位只返回后一张");
        ctx.SourceCard = owner.GetField()[1];
        adjacent = (List<CombatCard>)select.Call(ctx, "Adjacent")[0];
        Check(adjacent.SequenceEqual([owner.GetField()[0], owner.GetField()[2]]), "场上顺序中间返回前后卡");
        var enemies = (List<CombatCard>)select.Call(ctx, "TargetAll")[0];
        Check(enemies.SequenceEqual(opponent.GetField()), "对手全选只返回该对手场上卡");
        var random = (List<CombatCard>)select.Call(ctx, "TargetRandom")[0];
        Check(random.Count == 1 && enemies.Contains(random[0]), "随机选取一张真实对手卡");
        Check(((List<CombatCard>)select.Call(new APIContext(), "TargetAll")[0]).Count == 0, "空目标返回空列表");
    }

    private static void VerifyWoundSelection(Lua lua, CombatNpc owner)
    {
        var ctx = new APIContext { Caster = owner, Scene = owner.Scene };
        using var remove = lua.GetFunction("RemoveRandomWound");
        var small = owner.AddWound("bp_wound_small");
        owner.DealFieldChange();
        remove.Call(ctx, "Self", 2, true);
        owner.DealFieldChange();
        Check(owner.GetField().Contains(small), "精确移除 2 格伤口时保留 1 格伤口");
        remove.Call(ctx, "Self", 2, false);
        owner.DealFieldChange();
        Check(!owner.GetField().Contains(small), "至多 2 格的移除可选择 1 格伤口");
    }

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

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TCA 对齐断言失败：" + message);
        _assertions++;
    }
}
