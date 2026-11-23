using System.Text.Json;
using System.Text.RegularExpressions;
using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests;

public static partial class TcaAlignmentTests
{
    private static void VerifyCatalog(Lua lua, string dataDir)
    {
        VerifyJsonCatalog([Path.Combine(dataDir, "TriggerDefines.json")],
            TriggerDefineMgr.Instance.GetAll().Select(entry => entry.ID));
        Check(!LuaMgr.HookToEventId.ContainsValue("trigger_none"), "空触发不会生成 Lua Hook 映射");

        var actions = ActionDefineMgr.Instance.GetAll().ToArray();
        var actionIds = VerifyJsonCatalog(Directory.EnumerateFiles(Path.Combine(dataDir, "Action"), "*.json"),
            actions.Select(entry => entry.ID));
        Check(actions.Any(entry => entry.FuncName == "RecoverMP")
            && actions.Any(entry => entry.FuncName == "ReduceMP"), "目录收录直接增减 MP");
        Check(actions.All(entry => entry.FuncName is not ("ModifyContest" or "NextContestBonus"))
            && APIMgr.Instance.Get("ModifyContest") == null && APIMgr.Instance.Get("NextContestBonus") == null,
            "拼点强化使用 Buff，不发布通用槽内改值及下次改值接口");
        Check(APIMgr.Instance.Get("ScheduleRoundAction") == null && lua["ScheduleRoundAction"] == null
            && APIMgr.Instance.Get("AddRoundBuff") == null && lua["AddRoundBuff"] == null,
            "已移除两个轮次动作的 API 和 Lua 包装");
        Check(actions.All(entry => entry.ID != "action_add_npc_buff"), "通用拼点模板已由用途明确的动作替代");
        var deflectionApi = APIMgr.Instance.Get("AddDeflection")?.Method
            ?.GetCustomAttributes(typeof(APIFuncAttribute), false).FirstOrDefault();
        Check(actions.Any(entry => entry.ID == "action_add_deflection" && entry.FuncName == "AddDeflection")
            && actions.Where(entry => entry.FuncName == "AddNpcBuff").SelectMany(entry => entry.ParamDefs)
                .Where(param => param.Name == "BuffId").All(param => param.Value.All(value => value.GetString() != "Deflection"))
            && deflectionApi is APIFuncAttribute { ApiType: APIType.Action },
            "卸力通过独立防御动作选择");
        foreach (var entry in actions.Where(entry => actionIds.Contains(entry.ID)))
            VerifyTemplate(lua, entry.ID, entry.LuaTemplate, entry.ParamDefs, entry.FuncName);

        var conditions = ConditionDefineMgr.Instance.GetAll().ToArray();
        var conditionIds = VerifyJsonCatalog(Directory.EnumerateFiles(Path.Combine(dataDir, "Condition"), "*.json"),
            conditions.Select(entry => entry.ID));
        using var hasKeyword = lua.GetFunction("HasKeyword");
        var probe = new CombatCard();
        var query = new APIContext();
        foreach (var word in conditions.Single(c => c.ID == "cond_self_has_keyword").ParamDefs
            .Single(p => p.Name == "Keyword").Value.Select(value => value.GetString()!))
        {
            probe.BaseData.Keywords.Add(word);
            Check(LuaMgr.Instance.GetKeyword(word) != null && (bool)hasKeyword.Call(query, probe, word)[0],
                "关键词变体可检查已注册的 " + word);
            probe.BaseData.Keywords.Remove(word);
            Check(!(bool)hasKeyword.Call(query, probe, word)[0], "关键词变体读取本卡实际词表 " + word);
        }
        foreach (var entry in conditions.Where(entry => entry.ID != "cond_none" && conditionIds.Contains(entry.ID)))
            VerifyTemplate(lua, entry.ID, entry.LuaTemplate, entry.ParamDefs, entry.LuaTemplate.Split('(')[0]);
    }

    private static void VerifyValueBuffActions(Lua lua, CombatNpc owner, CombatNpc opponent)
    {
        var defines = CombatNpcModifierDefineMgr.Instance;
        Check(defines.Get("Ci_Changed_Once").DisplayName == "单次刺击强化"
            && defines.Get("Zhan_DamageIn_Changed").DisplayName == "斩击抗性"
            && defines.Get("Zhan_DamageIn_Changed_Once").DisplayName == "单次斩击抗性"
            && defines.Get("Zhan_DamageOut_Changed").DisplayName == "斩击伤害提高"
            && defines.Get("Zhan_DamageOut_Changed_Once").DisplayName == "单次斩击伤害提高",
            "拼点强化、伤害提高和抗性按约定显示中文名");
        string[] attacks = ["Attack", "Zhan", "Ci", "Da", "SheJi", "Jin", "Mu", "Shui", "Huo", "Tu"];
        string[] defenses = ["Defend", "Shield", "Block", "Dodge"];
        string[] damage = ["All", .. attacks.Skip(1)];
        var cases = new (string Prefix, CombatNpc Target, int Sign, CombatValueStage Stage, string[] Ranges)[]
        {
            ("action_self_attack_contest_boost", owner, 1, CombatValueStage.Contest, attacks),
            ("action_target_attack_contest_weaken", opponent, -1, CombatValueStage.Contest, attacks),
            ("action_self_defense_contest_boost", owner, 1, CombatValueStage.Contest, defenses),
            ("action_target_defense_contest_weaken", opponent, -1, CombatValueStage.Contest, defenses),
            ("action_self_damage_boost", owner, 1, CombatValueStage.DamageOut, damage),
            ("action_target_damage_weaken", opponent, -1, CombatValueStage.DamageOut, damage),
            ("action_self_damage_resist", owner, -1, CombatValueStage.DamageIn, damage),
            ("action_target_damage_vulnerability", opponent, 1, CombatValueStage.DamageIn, damage),
            ("action_self_heal_boost", owner, 1, CombatValueStage.HealOut, ["All"]),
            ("action_target_heal_received_weaken", opponent, -1, CombatValueStage.HealIn, ["All"]),
            ("action_self_shield_gain_boost", owner, 1, CombatValueStage.ShieldGain, ["All"]),
            ("action_target_shield_gain_weaken", opponent, -1, CombatValueStage.ShieldGain, ["All"]),
            ("action_self_mp_recovery_boost", owner, 1, CombatValueStage.MpRecovery, ["All"]),
            ("action_target_mp_recovery_weaken", opponent, -1, CombatValueStage.MpRecovery, ["All"])
        };

        foreach (var (prefix, target, sign, stage, ranges) in cases)
        foreach (bool once in new[] { false, true })
        {
            string suffix = once ? "_once" : "";
            var action = ActionDefineMgr.Instance.Get(prefix + suffix);
            Check(action?.FuncName == "AddNpcBuff", prefix + suffix + " 使用通用施加接口");
            Check(action!.DisplayName.Contains("单次") == once && !action.DisplayName.Contains("下次"),
                prefix + suffix + " 的时机显示为持续或单次");
            var axes = stage == CombatValueStage.Contest ? "" : "_" + stage;
            var expectedIds = ranges.Select(range => range + axes + "_Changed" + (once ? "_Once" : "")).ToArray();
            var idParam = action.ParamDefs.SingleOrDefault(param => param.Name == "BuffId");
            if (expectedIds.Length == 1)
            {
                Check(idParam == null && action.LuaTemplate.Contains($"\"{expectedIds[0]}\"", StringComparison.Ordinal),
                    action.ID + " 固定使用对应的状态");
            }
            else
            {
                var candidates = idParam!.Value.Select(value => value.GetString()).ToArray();
                Check(candidates.Length == expectedIds.Length && candidates.ToHashSet().SetEquals(expectedIds),
                    action.ID + " 只提供对应阶段、范围及期限的完整候选");
            }

            for (int i = 0; i < expectedIds.Length; i++)
            {
                var define = defines.Get(expectedIds[i]);
                var change = define?.ValueChange;
                Check(change != null && change.Range == ranges[i] && change.Stage == stage && change.Once == once
                    && define!.DisplayName.StartsWith("单次", StringComparison.Ordinal) == once,
                    action.ID + " 候选定义有效 " + expectedIds[i]);
            }

            string selected = expectedIds[^1];
            var template = action.LuaTemplate.Replace("{BuffId}", selected).Replace("{Stacks}", "2").Replace("{Duration}", "30");
            lua["sampleContext"] = new APIContext { Caster = owner, Scene = owner.Scene };
            lua.DoString("local ctx = sampleContext\n" + template);
            var modifier = target.GetAllModifiers().SingleOrDefault(mod => mod.DefineId == selected);
            var other = target == owner ? opponent : owner;
            Check(modifier?.CurrentStack == 2 * sign && modifier.Duration == (once ? -1 : 30)
                && other.GetAllModifiers().All(mod => mod.DefineId != selected),
                action.ID + " 生成 Lua 的挂载目标、层数和时限正确");
            target.RemoveModifier(selected);
        }
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
                if (entry.TryGetProperty("Tags", out var tags) && tags.EnumerateArray().Any(tag => tag.GetString() == "进阶"))
                {
                    var weight = entry.TryGetProperty("Weight", out var actionWeight) ? actionWeight : entry.GetProperty("weight");
                    var desc = entry.TryGetProperty("Desc", out var actionDesc) ? actionDesc : entry.GetProperty("desc");
                    Check(weight.GetDouble() == 0 && status == "Ready" &&
                        desc.GetString()!.Contains("进阶内容，有调整可能"), id + " 进阶记录可手动执行且权重为零");
                }
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
            var placeholder = Regex.Match(args[i], @"^-?""?\{(\w+)\}""?$");
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
            template = template.Replace("{" + parameter.Name + "}", value.ValueKind == JsonValueKind.String
                ? value.GetString()! : value.GetRawText(), StringComparison.Ordinal);
        }
        var ctx = new APIContext();
        if (template.Contains("ctx.SourceCard", StringComparison.Ordinal))
            ctx.SourceCard = (CombatCard)lua["templateCard"];
        lua["sampleContext"] = ctx;
        lua.DoString("local ctx = sampleContext\nlocal self = {m_Self = templateCard, m_Owner = templateNpc}\n" + template);
        foreach (var param in input)
            Check(ctx.GetObject(param.Name) != null, id + " Lua 包装传递 " + param.Name);
    }
}
